namespace OneBot.Interfaces
{
    public interface IDBUser<TUser, TParameter> : IDB
    {
        public Task<TUser?> GetUser(TParameter parameter);
        public Task<TUser> CreateUser(TParameter parameter);
    }

    public interface IDB : IDisposable
    {

    }
}
