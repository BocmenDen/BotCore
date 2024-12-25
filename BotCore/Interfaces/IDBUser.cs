namespace BotCore.Interfaces
{
    public interface IDBUser<TUser, TParameter> : IDB
        where TUser : IUser
    {
        public Task<TUser?> GetUser(TParameter parameter);
        public Task<TUser> CreateUser(TParameter parameter);
    }

    public interface IDB : IDisposable
    {

    }
}
