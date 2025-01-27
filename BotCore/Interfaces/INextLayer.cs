namespace BotCore.Interfaces
{
    public interface INextLayer<out TUser, out TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public event Func<TContext, Task> Update;
    }
}