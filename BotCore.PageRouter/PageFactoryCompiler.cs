using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;
#if DEBUG
using AgileObjects.ReadableExpressions;
#endif

namespace BotCore.PageRouter
{
    public static class PageFactoryCompiler
    {
        public const string DefaultCompiler = "default";

        private readonly static IReadOnlyList<MethodInfo> _itemsDefaultCompiler;
        private readonly static Dictionary<string, MethodInfo> _compilers = [];
        private readonly static MethodInfo _stopingCompile;
        private readonly static MethodInfo _startingCompile;

        private readonly static MethodInfo _createService = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider)])!;

        static PageFactoryCompiler()
        {
            List<MethodInfo> itemCompiler = [];
            foreach (var method in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)))
            {
                if (method.IsGenericMethod && method.GetCustomAttribute<PageFactoryCompilerFuncAttribute>() != null)
                {
                    itemCompiler.Add(method);
                    continue;
                }

                var attCompiler = method.GetCustomAttribute<PageFactoryCompilerAttribute>();
                if (attCompiler != null)
                    _compilers.Add(attCompiler.Name, method);
            }
            _itemsDefaultCompiler = itemCompiler;
            _stopingCompile = typeof(PageFactoryCompiler).GetMethod(nameof(StoppingCompileEvent), BindingFlags.Static | BindingFlags.NonPublic)!;
            _startingCompile = typeof(PageFactoryCompiler).GetMethod(nameof(StartingCompileEvent), BindingFlags.Static | BindingFlags.NonPublic)!;
        }

        public static Func<IServiceProvider, TUser, IPage<TUser, TContext>> Build<TUser, TContext>(Type type, string? name = null, ILogger? logger = null)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
        {
            if (name == null || name == DefaultCompiler || !_compilers.TryGetValue(name, out var compiler))
                return BuildNew<TUser, TContext>(type, logger);

            if (compiler.IsGenericMethod)
                compiler = compiler.MakeGenericMethod(ApplayGenericArguments<TUser, TContext>(compiler.GetGenericArguments()));

            return (Func<IServiceProvider, TUser, IPage<TUser, TContext>>)compiler.CreateDelegate(typeof(Func<TUser, IServiceProvider, IPage<TUser, TContext>>));
        }

        [PageFactoryCompiler(DefaultCompiler)]
        private static Func<IServiceProvider, TUser, IPage<TUser, TContext>> BuildNew<TUser, TContext>(Type type, ILogger? logger = null)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
        {
            return BuildHelper<TUser, TContext>(type, (provider, typePage) =>
            {
                var constructor = typePage.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (constructor != null)
                    return Expression.New(constructor);
                return Expression.Call(_createService.MakeGenericMethod(typePage), provider);
            }, logger: logger);
        }

        public static Func<IServiceProvider, TUser, IPage<TUser, TContext>> BuildHelper<TUser, TContext>(Type type, Func<ParameterExpression, Type, Expression> instancePage, Func<ParameterExpression, ParameterExpression, List<Expression>>? additionalActions = null, ILogger? logger = null)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
        {
            if (type.IsGenericType)
                type = type.MakeGenericType(ApplayGenericArguments<TUser, TContext>(type.GetGenericArguments()));

            if (!type.IsAssignableTo(typeof(IPage<TUser, TContext>))) throw new Exception($"Тип {type} не может быть приведён к {typeof(IPage<TUser, TContext>)}");
            List<Expression> expressions = [];
            ParameterExpression varUser = Expression.Parameter(typeof(TUser));
            ParameterExpression varServiceProvider = Expression.Parameter(typeof(IServiceProvider));
            var newExpr = instancePage(varServiceProvider, type);

            var varPage = Expression.Variable(type, "page");
            expressions.Add(Expression.Assign(varPage, newExpr));

            ApplayCompiler<TUser>(_startingCompile, type, varPage, varServiceProvider, varUser, expressions);

            foreach (var itemCompiler in _itemsDefaultCompiler)
                ApplayCompiler<TUser>(itemCompiler, type, varPage, varServiceProvider, varUser, expressions);

            if (additionalActions != null)
                expressions.AddRange(additionalActions(varPage, varUser));

            ApplayCompiler<TUser>(_stopingCompile, type, varPage, varServiceProvider, varUser, expressions);

            expressions.Add(varPage);

            var block = Expression.Block([varPage], expressions);
            var lambda = Expression.Lambda<Func<IServiceProvider, TUser, IPage<TUser, TContext>>>(block, varServiceProvider, varUser);

#if DEBUG
            logger?.LogDebug("Результат компиляции страницы {page}: {compileResult}", type.FullName, lambda.ToReadableString());
#endif

            return lambda.Compile();
        }

        private static void ApplayCompiler<TUser>(MethodInfo method, Type type, Expression page, Expression serviceProvider, Expression user, List<Expression> body)
            where TUser : IUser
        {
            List<Expression> parameters = [];

            if (method.IsGenericMethod)
            {
                var arguments = method.GetGenericArguments();
                if (arguments.Length != 1 || !arguments[0].GetGenericParameterConstraints().Contains(typeof(IUser))) return;
                method = method.MakeGenericMethod(typeof(TUser));
            }

            foreach (var parameter in method.GetParameters())
            {
                var parameterType = parameter.ParameterType;

                if (typeof(TUser).IsAssignableTo(parameterType))
                {
                    parameters.Add(user);
                    continue;
                }

                if (parameterType.IsAssignableTo(typeof(Attribute)))
                {
                    var attr = type.GetCustomAttribute(parameterType);
                    if (attr == null) return;
                    parameters.Add(Expression.Constant(attr, parameterType));
                    continue;
                }

                if (type.IsAssignableTo(parameterType))
                {
                    parameters.Add(page);
                    continue;
                }


                parameters.Add(Expression.Call(_createService.MakeGenericMethod(parameterType), serviceProvider));
            }

            body.Add(Expression.Call(method, parameters));
        }
        private static Type[] ApplayGenericArguments<TUser, TContext>(IEnumerable<Type> genericArguments)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
        {
            List<Type> result = [];
            foreach (var genericArgument in genericArguments)
            {
                if (genericArgument.GetGenericParameterConstraints().Contains(typeof(IUser)))
                {
                    result.Add(typeof(TUser));
                    continue;
                }

                if (!genericArgument.GetGenericParameterConstraints().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IUpdateContext<>)))
                    throw new Exception($"Не удалось найти тип для одного из generic параметров");
                result.Add(typeof(TContext));
            }
            return [.. result];
        }

        private static void StoppingCompileEvent<TUser>(IPageLoaded<TUser> page, TUser user) where TUser : IUser => page.PageLoaded(user);
        private static void StartingCompileEvent<TUser>(IPageLoading<TUser> page, TUser user) where TUser : IUser => page.PageLoading(user);
    }
}
