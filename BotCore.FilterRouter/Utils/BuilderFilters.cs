﻿//#if DEBUG
//using AgileObjects.ReadableExpressions;
//#endif
using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.FilterRouter.Utils
{
    public static class BuilderFilters
    {
        public static Func<IServiceProvider, TContext, EvaluatedAction> CompileFilters<TUser, TContext>(ILogger? logger, MethodInfo method)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
        {
            try
            {
                if (method.IsGenericMethod) throw new Exception("Метод не может быть обобщенным");
                WriterExpression<TUser> writerExpression = new(typeof(TContext));
                LabelTarget skipOtherFilters = Expression.Label(typeof(EvaluatedAction), "skipOtherFilters");
                ApplayFilters(writerExpression, method, skipOtherFilters, out var resultFilter, out var valuesFilters);
                var methodCall = ApplayArguments<TUser, TContext>(writerExpression, method, valuesFilters);
                var methodFunc = CastMethodToFunction(writerExpression, method, methodCall);
                var result = Expression.New(
                    typeof(EvaluatedAction).GetConstructors()[0],
                    resultFilter,
                    methodFunc
                );
                writerExpression.WriteBody(Expression.Return(skipOtherFilters, result));
                writerExpression.WriteBody(Expression.Label(skipOtherFilters, Expression.Constant(new EvaluatedAction(false, null))));
                Expression finalBlock = Expression.Block(writerExpression.GetParameterExpressions(), writerExpression);
                Expression<Func<IServiceProvider, TContext, EvaluatedAction>> finalLambda =
                    Expression.Lambda<Func<IServiceProvider, TContext, EvaluatedAction>>(
                            finalBlock,
                            [writerExpression.ServiceProvider, writerExpression.ContextParameter]
                        );
                var resultCompile = finalLambda.Compile();
                //#if DEBUG
                //                logger?.LogDebug("Фильтр {method} успешно скомпилирован в {expression}", method, finalLambda.ToReadableString());
                //#else
                logger?.LogDebug("Фильтр {method} успешно скомпилирован", method);
                //#endif
                return resultCompile;
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Ошибка при компиляции фильтра {method}", method);
                throw;
            }
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
            writerExpression.RegisterNoCacheParameter(resultFilter);
            var attributes = method.GetCustomAttributes<BaseFilterAttribute<TUser>>();
            var noResultAttributes = attributes.Where(x => !x.IsReturnValue);
            if (noResultAttributes.Any())
            {
                Expression combineFlags = Expression.Constant(false);
                foreach (var filter in noResultAttributes)
                    combineFlags = Expression.OrElse(combineFlags, filter.GetExpression(writerExpression));
                writerExpression.WriteBody(Expression.Assign(resultFilter, combineFlags));
                writerExpression.WriteBody(Expression.IfThen(resultFilter, Expression.Return(skipOtherFilters, Expression.Constant(new EvaluatedAction(true, null)))));
            }
            else
            {
                writerExpression.WriteBody(Expression.Assign(resultFilter, Expression.Constant(false)));
            }
            if (attributes == null || !attributes.Any())
            {
                writerExpression.WriteBody(Expression.Assign(resultFilter, Expression.Constant(false)));
                return;
            }
            ConstantExpression constantExitFalseExpressionLambda1 = Expression.Constant(false);
            foreach (BaseFilterAttribute<TUser> filter in attributes.Where(x => x.IsReturnValue))
            {
                ParameterExpression parameter = (filter.GetExpression(writerExpression) as ParameterExpression)!;
                bool searchFlag = valuesFilters.Any(x => x == parameter);
                valuesFilters.Add(parameter);
                if (searchFlag) continue;
                Expression flagProperty;
                if (parameter.Type.IsGenericType && parameter.Type.GetGenericTypeDefinition() == typeof(FilterResult<>))
                    flagProperty = Expression.Field(parameter, nameof(FilterResult<object>.IsSuccess));
                else if (parameter.Type == typeof(bool))
                    flagProperty = parameter;
                else
                    throw new Exception($"Возвращаемый тип не поддерживается у атрибута {filter}");
                writerExpression.WriteBody(Expression.IfThen(flagProperty, Expression.Block(Expression.Assign(resultFilter, flagProperty), Expression.Return(skipOtherFilters, Expression.Constant(new EvaluatedAction(true, null))))));
            }
        }

        private static MethodCallExpression ApplayArguments<TUser, TContext>(
                WriterExpression<TUser> writerExpression,
                MethodInfo method,
                List<ParameterExpression?> valuesFilters
            )
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
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
                if (typeof(TContext).IsAssignableTo(currentParametr.ParameterType))
                {
                    inputParametrs.Add(writerExpression.ContextParameter);
                    continue;
                }
                var attr = currentParametr.GetCustomAttribute<FilterResultPositionAttribute>();
                if (attr != null)
                {
                    if (attr.Position >= valuesFilters.Count) throw new Exception($"Позиция параметра {attr.Position} превышает количество результатов от фильтров");
                    var valueFilter = valuesFilters[attr.Position]!;
                    if (!valueFilter.Type.IsGenericType || valueFilter.Type.GetGenericTypeDefinition() != typeof(FilterResult<>))
                        throw new Exception($"Параметр с позицией {attr.Position} отмечен как результат фильтра но он не реализован через FilterResult");
                    if (valueFilter.Type.GenericTypeArguments.First() != currentParametr.ParameterType)
                        throw new Exception($"Тип параметра с позицией {attr.Position} не совпадает с типом указанным в FilterResult");
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
                throw new Exception("Тип возвращаемого значения метода не поддерживается");
            }
            return Expression.Lambda(result);
        }
    }
}
