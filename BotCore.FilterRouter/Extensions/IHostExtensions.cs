using BotCore.FilterRouter.Utils;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotCore.FilterRouter.Extensions
{
    public static class IHostExtensions
    {
        public static IHostBuilder RegisterFiltersRouter<TUser, TContext>(this IHostBuilder builder, IConditionalActionCollection<TUser, TContext> conditionalActionCollection)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            => builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(conditionalActionCollection);
            });

        public static IHostBuilder RegisterFiltersRouterAuto<TUser, TContext>(this IHostBuilder builder)
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
            => builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(typeof(IConditionalActionCollection<TUser, TContext>), (sp) =>
                {
                    ILogger logger = sp.GetRequiredService<ILogger<ConditionalActionCollectionBuilder<TUser, TContext>>>();
                    return ConditionalActionCollectionBuilder<TUser, TContext>.CreateAutoDetectFromCurrentDomain(logger).Build();
                });
            });
    }
}
