using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;
using BotCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.PageRouter
{
    internal static class DefaultNodeCompilers
    {
        internal static IEnumerable<Expression> StoppingCompileEvent(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IPageLoaded<>).MakeGenericType(parameters.TypeUser), nameof(IPageLoaded<IUser>.PageLoaded), out MethodInfo? method))
                yield break;
            yield return Expression.Call(parameters.Page, method!, parameters.User);
        }
        internal static IEnumerable<Expression> StartingCompileEvent(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IPageLoading<>).MakeGenericType(parameters.TypeUser), nameof(IPageLoading<IUser>.PageLoading), out MethodInfo? method))
                yield break;
            yield return Expression.Call(parameters.Page, method!, parameters.User);
        }
        [PageFactoryCompilerFunc]
        internal static IEnumerable<Expression> BindNavigateParameter(ParametersItemDefaultCompiler parameters)
        {
            if (!parameters.TypePage.TryGetInterfaceMethod(typeof(IBindNavigateParameter), nameof(IBindNavigateParameter.BindNavigateParameter), out MethodInfo? method))
                yield break;
            var typeService = typeof(IDBUserPageParameter<>).MakeGenericType(parameters.TypeUser);
            var parameter = Expression.Call(Expression.Constant(parameters.ServiceProvider.GetRequiredService(typeService), typeService), typeService.GetMethod(nameof(IDBUserPageParameter<IUser>.GetParameter))!, parameters.User);
            yield return Expression.Call(parameters.Page, method!, parameter);
            yield break;
        }
        [PageFactoryCompilerFunc]
        internal static IEnumerable<Expression> BindModel(ParametersItemDefaultCompiler parameters)
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
        internal static IEnumerable<Expression> BindNavigateFunction(ParametersItemDefaultCompiler parameters)
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
        [PageFactoryCompilerFunc]
        internal static IEnumerable<Expression> BindService(ParametersItemDefaultCompiler parameters)
        {
            foreach (var pageInterface in parameters.TypePage.GetInterfaces())
            {
                if (!pageInterface.IsGenericType || pageInterface.GetGenericTypeDefinition() != typeof(IBindService<>)) continue;
                var serviceType = pageInterface.GenericTypeArguments[0];
                if (parameters.TypePage.TryGetInterfaceMethod(pageInterface, nameof(BindService), out var method))
                {
                    yield return Expression.Call(parameters.Page, method!, parameters.GetServiceDynamic(serviceType));
                }
            }
        }
        [PageFactoryCompilerFunc]
        internal static IEnumerable<Expression> BindUser(ParametersItemDefaultCompiler parameters)
        {
            var pageInterface = parameters.TypePage.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBindUser<>));
            if (pageInterface != null)
            {
                var serviceType = pageInterface.GenericTypeArguments[0];
                if (serviceType != parameters.TypeUser) yield break;
                if (parameters.TypePage.TryGetInterfaceMethod(pageInterface, nameof(BindUser), out var method))
                {
                    yield return Expression.Call(parameters.Page, method!, parameters.User);
                }
            }
        }
    }
}
