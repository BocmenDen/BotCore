﻿using BotCore.Attributes;
using BotCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace BotCore.EfDb
{
    public static class IHostExtensions
    {
        private readonly static object PropertyConnectToDB = "BotCore.EfUserDb.IHostExtensions_ConnectToDB";
        private static readonly MethodInfo AddDbContextFactoryMethod = typeof(EntityFrameworkServiceCollectionExtensions).GetMethod(
            nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory),
            1,
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(IServiceCollection), typeof(Action<IServiceProvider, DbContextOptionsBuilder>), typeof(ServiceLifetime)],
            null)!;

        public static IHostBuilder RegisterDBContextOptions(this IHostBuilder builder, Action<IServiceProvider, IConfiguration, DbContextOptionsBuilder> optionsBuilder)
        {
            builder.Properties[PropertyConnectToDB] = optionsBuilder;
            return builder;
        }

        [ServiceRegisterProvider(DBAttribute.DBRegistrationProvaderName)]
        internal static void AddEFPool(HostBuilderContext context, IServiceCollection services, Type[] serviceTypes, Type implementationType)
        {
            if (serviceTypes.Length > 1) throw new Exception("Для БД количество представлений в данный момент не может быть множественным"); // TODO Fix

            if (!(context.Properties.TryGetValue(PropertyConnectToDB, out object? value) && value is Action<IServiceProvider, IConfiguration, DbContextOptionsBuilder> dbBuilder))
                throw new InvalidOperationException("Не удаётся зарегистрировать БД т.к. не указаны параметры подключения с помощью RegisterDBContextOptions");

            Action<IServiceProvider, DbContextOptionsBuilder> dbBuilderApplayConfig = (s, b) => dbBuilder(s, context.Configuration, b);
            var method = AddDbContextFactoryMethod.MakeGenericMethod(implementationType);
            method.Invoke(null, [services, dbBuilderApplayConfig, ServiceLifetime.Singleton]);

            services.AddSingleton(typeof(IFactory<>).MakeGenericType(implementationType), typeof(DBFactory<>).MakeGenericType(implementationType));

            services.AddSingleton(typeof(IReset<>).MakeGenericType(implementationType), typeof(DBReset<>).MakeGenericType(implementationType));
        }
    }
}
