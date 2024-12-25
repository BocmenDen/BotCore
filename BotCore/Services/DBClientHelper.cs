using BotCore.Attributes;
using BotCore.Interfaces;

namespace BotCore.Services
{
    [Service(ServiceType.Singltone)]
    public class DBClientHelper<TUser, TDB, TParametr> : IDBUser<TUser, TParametr>
        where TUser : IUser
        where TDB : IDB
    {
        private readonly Func<TParametr, Task<TUser>> _createUser = default!;
        private readonly Func<TParametr, Task<TUser?>> _getUser = default!;
        private readonly Action? _disponse;
        public readonly TDB Originaldatabase;

        public DBClientHelper(TDB? database = default, IFactory<TDB>? factory = null, IDBClientParametrConverter<TParametr>? converter = null)
        {
            if (factory != null)
            {
                database = factory.Create();
                _disponse = database.Dispose;
            }
            Originaldatabase=database!;
            if (database != null)
            {
                if (database is IDBUser<TUser, TParametr> db)
                {
                    _getUser = db.GetUser!;
                    _createUser = db.CreateUser;
                    return;
                }
                else if (database is IDBUser<TUser, long> dbDefault)
                {
                    ArgumentNullException.ThrowIfNull(converter);
                    _getUser = (p) => dbDefault.GetUser(converter.ParametrConvert(p));
                    _createUser = (p) => dbDefault.CreateUser(converter.ParametrConvert(p));
                    return;
                }
            }
            throw new Exception("Не удалось найти необходимую БД");
        }

        public Task<TUser?> GetUser(TParametr parameter) => _getUser(parameter);
        public Task<TUser> CreateUser(TParametr parameter) => _createUser(parameter);

        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}