namespace BotCore.Interfaces
{
    public interface IDBUser<TUser, TParameter> : IDB
        where TUser : IUser
    {
        public ValueTask<TUser?> GetUser(TParameter parameter);
        public ValueTask<TUser> CreateUser(TParameter parameter);
    }

    public interface IDB : IDisposable
    {

    }
}
