using BotCore.Attributes;
using BotCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace BotCore.EfUserDb
{
    public static class IHostExtensions
    {
        private readonly static object PropertyConnectToDB = "BotCore.EfUserDb.IHostExtensions_ConnectToDB";
        private static readonly MethodInfo AddDbContextFactoryMethod = typeof(EntityFrameworkServiceCollectionExtensions).GetMethod(
            nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory),
            1,
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(ServiceLifetime)],
            null)!;
        public static IHostBuilder RegisterDBContextOptions(this IHostBuilder builder, Action<IConfiguration, DbContextOptionsBuilder> optionsBuilder)
        {
            builder.Properties[PropertyConnectToDB] = optionsBuilder;
            return builder;
        }
        public static IHostBuilder RegisterDBContextOptions(this IHostBuilder builder, Action<DbContextOptionsBuilder> optionsBuilder)
            => builder.RegisterDBContextOptions((_, b) => optionsBuilder(b));

        [ServiceRegisterProvider(DBAttribute.DBRegistrationProvaderName)]
        internal static void AddEFPool(HostBuilderContext context, IServiceCollection services, Type _, Type implementationType)
        {
            if (!(context.Properties.TryGetValue(PropertyConnectToDB, out object? value) && value is Action<IConfiguration, DbContextOptionsBuilder> dbBuilder))
                throw new InvalidOperationException("Не удаётся зарегестрировать БД т.к. не указаны параметры подключения с помощью RegisterDBContextOptions");

            Action<DbContextOptionsBuilder> dbBuilderApplayConfig = (b) => dbBuilder(context.Configuration, b);
            var method = AddDbContextFactoryMethod.MakeGenericMethod(implementationType);
            method.Invoke(null, [services, dbBuilderApplayConfig, ServiceLifetime.Singleton]);
            services.AddSingleton(typeof(IFactory<>).MakeGenericType(implementationType), typeof(DBFactory<>).MakeGenericType(implementationType));
        }
    }
}
