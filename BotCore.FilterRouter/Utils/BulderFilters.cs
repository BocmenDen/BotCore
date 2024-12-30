using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.FilterRouter.Utils
{
    public static class BulderFilters
    {
        public static Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction> CompileFilters<TUser>(MethodInfo method)
            where TUser : IUser
        {
            if (method.IsGenericMethod) throw new Exception("Метод не может быть обобщенным");
            WriterExpression<TUser> writerExpression = new();
            LabelTarget skipOtherFilters = Expression.Label(typeof(EvaluatedAction), "skipOtherFilters");
            ApplayFilters<TUser>(writerExpression, method, skipOtherFilters, out var resultFilter, out var valuesFilters);
            var methodCall = ApplayArguments<TUser>(writerExpression, method, valuesFilters);
            var methodFunc = CastMethodToFunction(writerExpression, method, methodCall);
            var result = Expression.New(
                typeof(EvaluatedAction).GetConstructors()[0],
                resultFilter,
                methodFunc
            );
            writerExpression.WriteBody(Expression.Return(skipOtherFilters, result));
            writerExpression.WriteBody(Expression.Label(skipOtherFilters, Expression.Constant(new EvaluatedAction(false, null))));
            Expression finalBlock = Expression.Block(writerExpression.GetParametrExpressions(), writerExpression);
            Expression<Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>> finalLambda =
                Expression.Lambda<Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>>(
                        finalBlock,
                        [writerExpression.ServiceProvider, writerExpression.ContextParametr]
                    );
            return finalLambda.Compile();
        }

        private static void ApplayFilters<TUser>(
                WriterExpression<TUser> writerExpression,
                MethodInfo method,
                LabelTarget skipOtherFilters,
                out ParameterExpression resultFilter,
                out List<ParameterExpression?> valuesFilters
            )
            where TUser : IUser
        {
            valuesFilters = [];
            resultFilter = Expression.Parameter(typeof(bool), "resultFilter");
            writerExpression.RegisterNoChacheParametr(resultFilter);
            writerExpression.WriteBody(Expression.Assign(resultFilter, Expression.Constant(false)));
            var attributes = method.GetCustomAttributes<BaseFilterAttribute<TUser>>();
            if (attributes == null || !attributes.Any()) return;
            ConstantExpression constantExitFalseExpressionLambda1 = Expression.Constant(false);
            Expression<Action> test = () => Console.WriteLine("Hello, World!");
            writerExpression.WriteBody(Expression.Call(
                typeof(Debug).GetMethods().Where(x => x.Name == nameof(Debug.WriteLine) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(object)).First(),
                writerExpression.ContextParametr
                ));
            foreach (ParameterExpression parameter in attributes.Select(x => x.GetExpression(writerExpression)))
            {
                bool searchFlag = valuesFilters.Any(x => x == parameter);
                valuesFilters.Add(parameter);
                if (searchFlag) continue;
                Expression flagProperty;
                if (parameter.Type.IsGenericType && parameter.Type.GetGenericTypeDefinition() == typeof(FilterResult<>))
                    flagProperty = Expression.Field(parameter, nameof(FilterResult<object>.IsSuccess));
                else if (parameter.Type == typeof(bool))
                    flagProperty = parameter;
                else
                    throw new Exception("Возвращаемый тип не поддерживается");
                writerExpression.WriteBody(Expression.IfThen(flagProperty, Expression.Block(Expression.Assign(resultFilter, flagProperty), Expression.Return(skipOtherFilters, Expression.Constant(new EvaluatedAction(true, null))))));
            }
        }

        private static MethodCallExpression ApplayArguments<TUser>(
                WriterExpression<TUser> writerExpression,
                MethodInfo method,
                List<ParameterExpression?> valuesFilters
            )
            where TUser : IUser
        {
            var parametrsMethodInfo = method.GetParameters();
            List<Expression> inputParametrs = [];
            for (int i = 0; i < parametrsMethodInfo.Length; i++)
            {
                var currentParametr = parametrsMethodInfo[i];
                if (currentParametr.ParameterType == typeof(IServiceProvider))
                {
                    inputParametrs.Add(writerExpression.ServiceProvider);
                    continue;
                }
                if (currentParametr.ParameterType == typeof(IUpdateContext<TUser>))
                {
                    inputParametrs.Add(writerExpression.ContextParametr);
                    continue;
                }
                var attr = currentParametr.GetCustomAttribute<FilterResultPositionAttribute>();
                if (attr != null)
                {
                    if (attr.Position >= valuesFilters.Count) throw new Exception("Позиция результата фильтра не найдена");
                    var valueFilter = valuesFilters[attr.Position]!;
                    if (!valueFilter.Type.IsGenericType || valueFilter.Type.GetGenericTypeDefinition() != typeof(FilterResult<>))
                        throw new Exception("Тип параметра не является FilterResult");
                    if (valueFilter.Type.GenericTypeArguments.First() != currentParametr.ParameterType)
                        throw new Exception("Тип параметра не совпадает с типом FilterResult");
                    inputParametrs.Add(Expression.Field(valueFilter, nameof(FilterResult<object>.Value)));
                    valuesFilters[attr.Position] = null;
                    continue;
                }
                var searchIndex = valuesFilters.FindIndex(x =>
                    x != null &&
                    x.Type.IsGenericType &&
                    x.Type.GetGenericTypeDefinition() == typeof(FilterResult<>) &&
                    x.Type.GenericTypeArguments[0] == currentParametr.ParameterType);
                if (searchIndex == -1)
                {
                    inputParametrs.Add(writerExpression.GetService(currentParametr.ParameterType));
                    continue;
                }
                inputParametrs.Add(Expression.Field(valuesFilters[searchIndex]!, nameof(FilterResult<object>.Value)));
                valuesFilters[searchIndex] = null;
            }
            return Expression.Call(null, method, inputParametrs);
        }

        private static LambdaExpression CastMethodToFunction<TUser>(
                WriterExpression<TUser> writerExpression,
                MethodInfo method,
                MethodCallExpression methodExpression
            )
            where TUser : IUser
        {
            ConstantExpression constantExitFalseExpressionLambda2 = Expression.Constant(Task.FromResult(false));
            ConstantExpression constantExitTrueExpressionLambda2 = Expression.Constant(Task.FromResult(true));
            Expression result;
            if (method.ReturnType == typeof(void))
            {
                writerExpression.WriteBody(methodExpression);
                result = constantExitTrueExpressionLambda2;
            }
            else if (method.ReturnType == typeof(bool))
            {
                result = Expression.Condition(
                    methodExpression,
                    constantExitTrueExpressionLambda2,
                    constantExitFalseExpressionLambda2
                    );
            }
            else if (method.ReturnType == typeof(Task))
            {
                Expression<Func<Task, bool>> lambdaExpression = (_) => true;
                var methodContinueWith = typeof(Task).GetMethods()
                    .Where(x => x.Name == nameof(Task.ContinueWith) && x.IsGenericMethod && x.GetParameters().Length == 1)
                    .First().MakeGenericMethod(typeof(bool));
                result = Expression.Call(methodExpression, methodContinueWith, Expression.Lambda(lambdaExpression.Body, lambdaExpression.Parameters));
            }
            else if (method.ReturnType == typeof(Task<bool>))
            {
                result = methodExpression;
            }
            else
            {
                throw new Exception("Тип возвращаемого значения не поддерживается");
            }
            return Expression.Lambda(result);
        }
    }
}
