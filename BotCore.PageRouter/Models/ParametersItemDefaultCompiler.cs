using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.PageRouter.Models
{
    public record ParametersItemDefaultCompiler
    {
        private readonly static MethodInfo _createService = typeof(ServiceProviderServiceExtensions)
            .GetMethod(nameof(ServiceProviderServiceExtensions.GetService), BindingFlags.Public | BindingFlags.Static, [typeof(IServiceProvider)])!;

        public readonly Type TypeKey;
        public readonly Type TypeContext;
        public readonly Type TypeUser;
        public readonly ParameterExpression User;
        public readonly Type TypePage;
        public readonly ParameterExpression Page;
        public readonly ParameterExpression Services;
        public readonly List<ParameterExpression> Variables = [];
        public readonly IServiceProvider ServiceProvider;

        public ParametersItemDefaultCompiler(IServiceProvider serviceProvider, Type typeKey, Type typeContext, Type typeUser, ParameterExpression user, Type typePage, ParameterExpression page, ParameterExpression services)
        {
            ServiceProvider=serviceProvider??throw new ArgumentNullException(nameof(serviceProvider));
            TypeKey=typeKey??throw new ArgumentNullException(nameof(typeKey));
            TypeContext=typeContext??throw new ArgumentNullException(nameof(typeContext));
            TypeUser=typeUser??throw new ArgumentNullException(nameof(typeUser));
            User=user??throw new ArgumentNullException(nameof(user));
            TypePage=typePage??throw new ArgumentNullException(nameof(typePage));
            Page=page??throw new ArgumentNullException(nameof(page));
            Services=services??throw new ArgumentNullException(nameof(services));
        }

        public Expression GetServiceDynamic(Type typeService)
            => Expression.Call(_createService.MakeGenericMethod(typeService), Services);
    }
}
