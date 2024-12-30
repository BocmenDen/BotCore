using BotCore.FilterRouter.Utils;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BotCore.FilterRouter.Extensions
{
    public static class IHostExtensions
    {
        public static IHostBuilder RegisterFiltersRouter<TUser>(this IHostBuilder builder, IConditionalActionCollection<TUser> conditionalActionCollection)
            where TUser : IUser
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(conditionalActionCollection);
            });
            return builder;
        }

        public static IHostBuilder RegisterFiltersRouterAuto<TUser>(this IHostBuilder builder)
            where TUser : IUser => builder.RegisterFiltersRouter(ConditionalActionCollectionBuilder<TUser>.CreateAutoDetectFromCurrentDomain().Build());
    }
}
