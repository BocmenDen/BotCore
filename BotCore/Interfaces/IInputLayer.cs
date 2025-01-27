namespace BotCore.Interfaces
{
    public interface IInputLayer<in TUser, in TContext>
            where TUser : IUser
            where TContext : IUpdateContext<TUser>
    {
        public Task HandleNewUpdateContext(TContext context);
    }
}