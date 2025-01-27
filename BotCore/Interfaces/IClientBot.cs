namespace BotCore.Interfaces
{
    public interface IClientBot<out TUser, out TContext> : IClientBotFunctions, INextLayer<TUser, TContext>, IDisposable
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public int Id { get; }

        public Task Run(CancellationToken token = default);
    }
}
