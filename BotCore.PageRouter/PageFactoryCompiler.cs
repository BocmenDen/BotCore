using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;
using BotCore.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Reflection;


//#if DEBUG
//using AgileObjects.ReadableExpressions;
//#endif

namespace BotCore.PageRouter
{
    public class PageFactoryCompiler
    {
        public const string DefaultCompiler = "default";
        public delegate IPage<TUser, TContext> CreatePage<TUser, TContext>(IServiceProvider serviceProvider, TUser user) where TUser : IUser where TContext : IUpdateContext<TUser>;

        private readonly static List<Func<ParametersItemDefaultCompiler, IEnumerable<Expression>>> _itemsDefaultCompiler = [];
        private readonly static Dictionary<string, MethodInfo> _compilers = [];
        private readonly static MethodInfo _stopingCompile;
        private readonly static MethodInfo _startingCompile;

        private readonly static MethodInfo _createService = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider)])!;

        static PageFactoryCompiler()
        {
            foreach (var method in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)))
            {
                if (method.GetCustomAttribute<PageFactoryCompilerFuncAttribute>() != null &&
                    method.ReturnType == typeof(IEnumerable<Expression>))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(ParametersItemDefaultCompiler)) continue;
                    _itemsDefaultCompiler.Add(method.CreateDelegate<Func<ParametersItemDefaultCompiler, IEnumerable<Expression>>>());
                    continue;
                }

                var attCompiler = method.GetCustomAttribute<PageFactoryCompilerAttribute>();
                if (attCompiler != null)
                    _compilers.Add(attCompiler.Name, method);
            }
            _stopingCompile = typeof(PageFactoryCompiler).GetMethod(nameof(StoppingCompileEvent), BindingFlags.Static | BindingFlags.NonPublic)!;
            _startingCompile = typeof(PageFactoryCompiler).GetMethod(nameof(StartingCompileEvent), BindingFlags.Static | BindingFlags.NonPublic)!;
        }

        public static CreatePage<TUser, TContext> Build<TUser, TContext, TKey>(Type type, TKey key, IServiceProvider serviceProvider, string? name = null)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            if (name == null || name == DefaultCompiler || !_compilers.TryGetValue(name, out var compiler))
                return BuildChachePage<TUser, TContext, TKey>(type, key, serviceProvider);

            if (compiler.IsGenericMethod)
                compiler = compiler.MakeGenericMethod(ApplayGenericArguments<TUser, TContext>(compiler.GetGenericArguments()));

            return (CreatePage<TUser, TContext>)compiler.Invoke(null, [type, key, serviceProvider])!;
        }

        [PageFactoryCompiler(DefaultCompiler)]
        private static CreatePage<TUser, TContext> BuildChachePage<TUser, TContext, TKey>(Type type, TKey key, IServiceProvider serviceProvider)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            var attr = type.GetCustomAttribute<PageCacheableAttribute<TKey>>();
            if (attr == null && !type.IsAssignableTo(typeof(IGetCacheOptions)))
                return BuildNew<TUser, TContext, TKey>(type, serviceProvider);

            var serviceObj = serviceProvider.GetService<IMemoryCache>();
            if (serviceObj == null)
            {
                serviceProvider.GetRequiredService<ILogger<PageFactoryCompiler>>().LogWarning("Не найден сервис {serviceName}, поэтому страница {typePage} не будет кэшироваться", nameof(IMemoryCache), type);
                return BuildNew<TUser, TContext, TKey>(type, serviceProvider);
            }
            var keyPageCtor = typeof(KeyPage<>).MakeGenericType(typeof(TKey)).GetConstructor([typeof(TKey), typeof(long)])!;
            var keyPage = Expression.Parameter(typeof(object), "keyPage");
            var service = Expression.Parameter(typeof(IMemoryCache), "memoryCache");

            IEnumerable<Expression> instancePage(ParametersItemDefaultCompiler parameters, LabelTarget returnLabel)
            {
                parameters.Variables.Add(keyPage);
                parameters.Variables.Add(service);

                var constructor = parameters.TypePage.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                var method = typeof(CacheExtensions).GetMethods().First(x => x.Name == nameof(CacheExtensions.Get) && x.IsGenericMethod).MakeGenericMethod(parameters.TypePage);

                yield return Expression.Assign(keyPage, Expression.Convert(Expression.New(keyPageCtor, Expression.Constant(key, typeof(TKey)), Expression.Property(parameters.User, nameof(IUser.Id))), typeof(object)));
                yield return Expression.Assign(service, Expression.Constant(serviceObj));

                yield return Expression.Assign(parameters.Page, Expression.Call(method, service, keyPage));
                yield return Expression.IfThen(Expression.NotEqual(parameters.Page, Expression.Constant(null, parameters.TypePage)), Expression.Return(returnLabel, parameters.Page));
                foreach (var nodeBody in InstancePageDefault(parameters, returnLabel))
                    yield return nodeBody;
                yield break;
            }

            IEnumerable<Expression> endBuild(ParametersItemDefaultCompiler parameters, LabelTarget returnLabel)
            {
                var method = typeof(CacheExtensions).GetMethods().First(x =>
                {
                    if (x.IsGenericMethod && x.Name == nameof(CacheExtensions.Set))
                    {
                        var parameters = x.GetParameters();
                        return parameters.Length == 4 && parameters[3].ParameterType == typeof(MemoryCacheEntryOptions);
                    }
                    return false;
                }).MakeGenericMethod(parameters.TypePage);
                Expression cacheOptions = attr == null ? Expression.Call(parameters.Page, type.GetInterfaceMethod(typeof(IGetCacheOptions), nameof(IGetCacheOptions.GetCacheOptions))) : Expression.Constant(new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = attr.SlidingExpiration
                }, typeof(MemoryCacheEntryOptions));
                yield return Expression.Call(method, service, keyPage, parameters.Page, cacheOptions);
            }

            return BuildHelper<TUser, TContext, TKey>(type, instancePage, serviceProvider, endBuild);
        }

        public static IEnumerable<Expression> InstancePageDefault(ParametersItemDefaultCompiler parameters, LabelTarget returnLabel)
        {
            var constructor = parameters.TypePage.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (constructor != null)
            {
                yield return Expression.Assign(parameters.Page, Expression.New(constructor));
                yield break;
            }
            yield return Expression.Assign(parameters.Page, Expression.Call(_createService.MakeGenericMethod(parameters.TypePage), parameters.Services));
        }

        private static CreatePage<TUser, TContext> BuildNew<TUser, TContext, TKey>(Type type, IServiceProvider serviceProvider)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            return BuildHelper<TUser, TContext, TKey>(type, InstancePageDefault, serviceProvider: serviceProvider);
        }

        public static CreatePage<TUser, TContext> BuildHelper<TUser, TContext, TKey>(Type type, Func<ParametersItemDefaultCompiler, LabelTarget, IEnumerable<Expression>> instancePage, IServiceProvider serviceProvider, Func<ParametersItemDefaultCompiler, LabelTarget, IEnumerable<Expression>>? additionalActions = null)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            where TKey : notnull
        {
            if (type.IsGenericType)
                type = type.MakeGenericType(ApplayGenericArguments<TUser, TContext>(type.GetGenericArguments()));

            if (!type.IsAssignableTo(typeof(IPage<TUser, TContext>))) throw new Exception($"Тип {type} не может быть приведён к {typeof(IPage<TUser, TContext>)}");
            List<Expression> expressions = [];

            ParametersItemDefaultCompiler parameters = new(serviceProvider, typeof(TKey), typeof(TContext), typeof(TUser), Expression.Parameter(typeof(TUser)), type, Expression.Variable(type, "page"), Expression.Parameter(typeof(IServiceProvider)));
            parameters.Variables.Add(parameters.Page);
            var returnLabel = Expression.Label(typeof(IPage<TUser, TContext>), "retVal");
            expressions.AddRange(instancePage(parameters, returnLabel));

            expressions.AddRange(StartingCompileEvent(parameters));
            foreach (var itemCompiler in _itemsDefaultCompiler)
                expressions.AddRange(itemCompiler(parameters));

            if (additionalActions != null)
                expressions.AddRange(additionalActions(parameters, returnLabel));

            expressions.AddRange(StoppingCompileEvent(parameters));

            expressions.Add(Expression.Label(returnLabel, parameters.Page));

            var block = Expression.Block(parameters.Variables, expressions);
            var lambda = Expression.Lambda<CreatePage<TUser, TContext>>(block, parameters.Services, parameters.User);

            //#if DEBUG
            //            serviceProvider.GetRequiredService<ILogger<PageFactoryCompiler>>()?.LogDebug("Результат компиляции страницы {page}: {compileResult}", type.FullName, lambda.ToReadableString());
            //#endif

            return lambda.Compile();
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

        private static IEnumerable<Expression> StoppingCompileEvent(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IPageLoaded<>).MakeGenericType(parameters.TypeUser), nameof(IPageLoaded<IUser>.PageLoaded), out MethodInfo? method))
                yield break;
            yield return Expression.Call(parameters.Page, method!, parameters.User);
        }
        private static IEnumerable<Expression> StartingCompileEvent(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IPageLoading<>).MakeGenericType(parameters.TypeUser), nameof(IPageLoading<IUser>.PageLoading), out MethodInfo? method))
                yield break;
            yield return Expression.Call(parameters.Page, method!, parameters.User);
        }
        [PageFactoryCompilerFunc]
        private static IEnumerable<Expression> BindNavigateParameter(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IBindNavigateParameter), nameof(IBindNavigateParameter.BindNavigateParameter), out MethodInfo? method))
                yield break;
            var typeService = typeof(IDBUserPageParameter<>).MakeGenericType(parameters.TypeUser);
            var parameter = Expression.Call(Expression.Constant(parameters.ServiceProvider.GetRequiredService(typeService), typeService), typeService.GetMethod(nameof(IDBUserPageParameter<IUser>.GetParameter))!, parameters.User);
            yield return Expression.Call(parameters.Page, method!, parameter);
            yield break;
        }
        [PageFactoryCompilerFunc]
        private static IEnumerable<Expression> BindModel(ParametersItemDefaultCompiler parameters)
        {
            var typeServiceNative = typeof(IDBUserPageModel<>).MakeGenericType(parameters.TypeUser);
            var typeServiceDefault = typeof(IDBUserPageModel<IUser>);
            var serviceObj = parameters.ServiceProvider.GetService(typeServiceNative) ??
                parameters.ServiceProvider.GetService(typeServiceDefault);
            Expression service = Expression.Constant(serviceObj, typeServiceNative);
            foreach (var @interface in parameters.TypePage.GetInterfaces())
            {
                if (!@interface.IsGenericType ||
                    @interface.GetGenericTypeDefinition() != typeof(IBindStorageModel<>) ||
                    !parameters.TypePage.TryGetInterfaceMethod(@interface, nameof(IBindStorageModel<object>.BindStorageModel), out MethodInfo? methodBind)) continue;

                if (serviceObj == null)
                    throw new Exception($"Не найден сервис-хранилище моделей страниц, {typeServiceNative} или {typeServiceDefault}");

                var typeArgument = @interface.GetGenericArguments()[0];
                var method = typeServiceNative.GetMethod(nameof(IDBUserPageModel<IUser>.GetModel))!.MakeGenericMethod(typeArgument);
                var model = Expression.Call(service, method, parameters.User);
                yield return Expression.Call(parameters.Page, methodBind!, model);
            }
        }
        [PageFactoryCompilerFunc]
        private static IEnumerable<Expression> BindNavigateFunction(ParametersItemDefaultCompiler parameters)
        {
            foreach (var @interface in parameters.TypePage.GetInterfaces())
            {
                if (!@interface.IsGenericType || @interface.GetGenericTypeDefinition() != typeof(IBindNavigateFunction<,,>)) continue;
                var typeInterfaceUser = @interface.GetGenericArguments()[0];
                var typeInterfaceContext = @interface.GetGenericArguments()[1];
                var typeInterfaceKey = @interface.GetGenericArguments()[2];
                if (!parameters.TypeUser.IsAssignableTo(typeInterfaceUser) || !parameters.TypeContext.IsAssignableTo(typeInterfaceContext) || !parameters.TypeKey.IsAssignableTo(typeInterfaceKey) ||
                    !parameters.TypePage.TryGetInterfaceMethod(@interface, nameof(IBindNavigateFunction<IUser, IUpdateContext<IUser>, object>.BindNavigateFunction), out MethodInfo? methodBind)) continue;

                var typeService = typeof(HandlePageRouter<,,>).MakeGenericType(parameters.TypeUser, parameters.TypeContext, parameters.TypeKey);
                var service = parameters.GetServiceDynamic(typeService);
                var methodService = typeService.GetMethod(nameof(HandlePageRouter<IUser, IUpdateContext<IUser>, object>.Navigate))!;

                var paramContext = Expression.Parameter(parameters.TypeContext, "context");
                var paramKey = Expression.Parameter(parameters.TypeKey, "key");
                var callNavigate = Expression.Call(service, methodService, paramContext, paramKey);
                var navigateFuncType = typeof(Func<,,>).MakeGenericType(parameters.TypeContext, parameters.TypeKey, methodService.ReturnType);
                var navigateDelegate = Expression.Lambda(navigateFuncType, callNavigate, paramContext, paramKey);
                yield return Expression.Call(parameters.Page, methodBind!, navigateDelegate);
                yield break;
            }
            yield break;
        }

        private readonly struct KeyPage<Key>(Key keyPage, long userId) : IEquatable<KeyPage<Key>> where Key : notnull
        {
            public readonly Key PageKey = keyPage;
            public readonly long UserId = userId;

            public override bool Equals(object? obj)
            {
                return obj is KeyPage<Key> page&&Equals(page);
            }

            public bool Equals(KeyPage<Key> other)
            {
                return EqualityComparer<Key>.Default.Equals(PageKey, other.PageKey)&&
                       UserId==other.UserId;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(PageKey, UserId);
            }

            public override readonly string? ToString() => $"{UserId}->{PageKey}";

            public static bool operator ==(KeyPage<Key> left, KeyPage<Key> right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(KeyPage<Key> left, KeyPage<Key> right)
            {
                return !(left==right);
            }
        }
    }
}
