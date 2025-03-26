using BotCore.Attributes;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace BotCore
{
    public static class BotBuilder
    {
        public delegate void RegisterService(HostBuilderContext context, IServiceCollection services, Type[] serviceTypes, Type implementationType);

        private readonly static Dictionary<string, RegisterService> _providersRegistrationService = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().SelectMany(t => GetMethods(t))).ToDictionary();
        private static IEnumerable<KeyValuePair<string, RegisterService>> GetMethods(Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var attr = method.GetCustomAttribute<ServiceRegisterProvider>();
                if (attr == null) continue;
                var parameters = method.GetParameters();
                if (parameters.Length == 4 &&
                    parameters[0].ParameterType == typeof(HostBuilderContext) &&
                    parameters[1].ParameterType == typeof(IServiceCollection) &&
                    parameters[2].ParameterType == typeof(Type[]) &&
                    parameters[3].ParameterType == typeof(Type))
                    yield return new(attr.ServiceName, method.CreateDelegate<RegisterService>());
            }
            yield break;
        }

        public static IHostBuilder CreateDefaultBuilder() => Host.CreateDefaultBuilder().RegisterServices(typeof(BotBuilder).Assembly);

        public static IHostBuilder RegisterServices(this IHostBuilder builder, Assembly? assembly)
        {
            if (assembly == null) return builder;
            builder.ConfigureServices((context, services) =>
            {
                foreach (var implementationType in assembly.GetTypes())
                {
                    ServiceAttribute? attr = implementationType.GetCustomAttribute<ServiceAttribute>();
                    if (attr == null) continue;
                    var register = _providersRegistrationService[attr.LifetimeType];
                    register(context, services, attr.Types == null || attr.Types.Length == 0 ? [implementationType] : attr.Types, implementationType);
                }
            });
            return builder;
        }
        public static IHostBuilder RegisterServices(this IHostBuilder builder, IEnumerable<Assembly?>? assemblies)
        {
            if (assemblies == null) return builder;
            foreach (var assembly in assemblies)
                builder.RegisterServices(assembly);
            return builder;
        }
        public static IHostBuilder RegisterServices(this IHostBuilder builder, params Assembly?[] assemblies) => builder.RegisterServices((IEnumerable<Assembly?>)assemblies);
        public static IHostBuilder RegisterClient<T>(this IHostBuilder builder)
            where T : class, IClientBot<IUser, IUpdateContext<IUser>>
        {
            if (!typeof(T).GetInterfaces().Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x).Contains(typeof(IClientBot<,>)))
                throw new InvalidOperationException($"Тип {typeof(T)} не наследует IClientBot<,>");
            return builder.ConfigureServices((s) =>
            {
                s.AddSingleton<T>();
                s.AddHostedService((serviceProvider) => serviceProvider.GetRequiredService<T>());
                s.AddSingleton(typeof(IClientBot), (serviceProvider) => serviceProvider.GetRequiredService<T>());
            });
        }

        [ServiceRegisterProvider(nameof(ServiceType.Singleton))]
        internal static void AddSingleton(HostBuilderContext _, IServiceCollection services, Type[] serviceTypes, Type implementationType)
        {
            var firstType = serviceTypes.First();
            services.AddSingleton(firstType, implementationType);
            foreach (var type in serviceTypes.Skip(1))
                services.AddSingleton(type, (serviceProvider) => serviceProvider.GetRequiredService(firstType));
        }
        [ServiceRegisterProvider(nameof(ServiceType.Scoped))]
        internal static void AddScoped(HostBuilderContext _, IServiceCollection services, Type[] serviceTypes, Type implementationType)
        {
            var firstType = serviceTypes.First();
            services.AddScoped(firstType, implementationType);
            foreach (var type in serviceTypes.Skip(1))
                services.AddScoped(type, (serviceProvider) => serviceProvider.GetRequiredService(firstType));
        }
        [ServiceRegisterProvider(nameof(ServiceType.Transient))]
        internal static void AddTransient(HostBuilderContext _, IServiceCollection services, Type[] serviceTypes, Type implementationType)
        {
            foreach (var type in serviceTypes)
                services.AddTransient(type, implementationType);
        }
        [ServiceRegisterProvider(nameof(ServiceType.Hosted))]
        internal static void AddHosted(HostBuilderContext _, IServiceCollection services, Type[] serviceTypes, Type implementationType)
        {
            services.AddSingleton(implementationType);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), (s) => s.GetRequiredService(implementationType)));
            foreach (var type in serviceTypes)
            {
                if (type == implementationType) continue;
                services.AddSingleton(type, (s) => s.GetRequiredService(implementationType));
            }
        }
    }
}