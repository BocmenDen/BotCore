using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneBot.Attributes;
using System.Reflection;

namespace OneBot.EfUserDb
{
    public static class IHostExtensions
    {
        private readonly static object PropertyConnectToDB = "OneBot.EfUserDb.IHostExtensions_ConnectToDB";

        private static readonly MethodInfo AddDbContextPoolMethod = typeof(EntityFrameworkServiceCollectionExtensions).GetMethod(
            nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool),
            1,
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(IServiceCollection), typeof(Action<DbContextOptionsBuilder>), typeof(int)],
            null)!;
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

            var method = AddDbContextPoolMethod.MakeGenericMethod(implementationType);
            Action<DbContextOptionsBuilder> dbBuilderApplayConfig = (b) => dbBuilder(context.Configuration, b);
            method.Invoke(null, [services, dbBuilderApplayConfig, 1024]);
            method = AddDbContextFactoryMethod.MakeGenericMethod(implementationType);
            method.Invoke(null, [services, dbBuilderApplayConfig, ServiceLifetime.Singleton]);
        }
    }
}
