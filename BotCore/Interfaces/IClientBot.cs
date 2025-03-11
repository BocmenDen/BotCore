using Microsoft.Extensions.Hosting;

namespace BotCore.Interfaces
{
    public interface IClientBot<out TUser, out TContext> : IClientBotFunctions, INextLayer<TUser, TContext>, IHostedService, IDisposable
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
    }
}
