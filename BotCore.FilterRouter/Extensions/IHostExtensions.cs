using BotCore.FilterRouter.Utils;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotCore.FilterRouter.Extensions
{
    public static class IHostExtensions
    {
        public static IHostBuilder RegisterFiltersRouter<TUser>(this IHostBuilder builder, IConditionalActionCollection<TUser> conditionalActionCollection)
            where TUser : IUser => builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(conditionalActionCollection);
            });

        public static IHostBuilder RegisterFiltersRouterAuto<TUser>(this IHostBuilder builder)
            where TUser : IUser => builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(typeof(IConditionalActionCollection<TUser>), (sp) =>
                {
                    ILogger logger = sp.GetRequiredService<ILogger<ConditionalActionCollectionBuilder<TUser>>>();
                    return ConditionalActionCollectionBuilder<TUser>.CreateAutoDetectFromCurrentDomain(logger).Build();
                });
            });
    }
}
