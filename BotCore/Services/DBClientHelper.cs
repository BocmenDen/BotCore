using BotCore.Attributes;
using BotCore.Interfaces;

namespace BotCore.Services
{
    [Service(ServiceType.Transient)]
    public class DBClientHelper<TUser, TDB, TParametr, TDBProvider> : IDBUser<TUser, TParametr>, IObjectProvider<TDB>
        where TUser : IUser
        where TDB : IDB
        where TDBProvider : IObjectProvider<TDB>
    {
        private readonly TDBProvider _dbProvider;

        private readonly Func<TParametr, Task<TUser>> _createUser;
        private readonly Func<TParametr, Task<TUser?>> _getUser;
        private readonly Func<TParametr, Task<(TUser user, bool isCreate)>> _getOrCreate;
        private readonly Action? _disponse;

        public DBClientHelper(TDBProvider dbProvider, IDBClientParametrConverter<TParametr>? converter = null)
        {
            _dbProvider = dbProvider;
            if (dbProvider is IDisposable disposable) _disponse = () => disposable.Dispose();
            if (dbProvider is IObjectProvider<IDBUser<TUser, TParametr>> castParametrDB)
            {
                _createUser = (p) => castParametrDB.TakeObject((db) => db.CreateUser(p));
                _getUser = (p) => castParametrDB.TakeObject((db) => db.GetUser(p));
                _getOrCreate = (p) => castParametrDB.TakeObject(async (db) =>
                {
                    var user = await db.GetUser(p);
                    bool isCreate = user == null;
                    return (user ?? await db.CreateUser(p), isCreate);
                });
                return;
            }
            else if (dbProvider is IObjectProvider<IDBUser<TUser, long>> castDefaultDB && converter != null)
            {
                _createUser = (p) => castDefaultDB.TakeObject((db) => db.CreateUser(converter.ParametrConvert(p)));
                _getUser = (p) => castDefaultDB.TakeObject((db) => db.GetUser(converter.ParametrConvert(p)));
                _getOrCreate = (p) => castDefaultDB.TakeObject(async (db) =>
                {
                    var id = converter.ParametrConvert(p);
                    var user = await db.GetUser(id);
                    bool isCreate = user == null;
                    return (user ?? await db.CreateUser(id), isCreate);
                });
                return;
            }
            throw new Exception("Не удалось найти подходящию БД");
        }

        public Task<TUser?> GetUser(TParametr parameter) => _getUser(parameter);
        public Task<TUser> CreateUser(TParametr parameter) => _createUser(parameter);
        public Task<(TUser user, bool isCreate)> GetOrCreate(TParametr parameter) => _getOrCreate(parameter);

        public void TakeObject(Action<TDB> func) => _dbProvider.TakeObject(func);
        public T TakeObject<T>(Func<TDB, T> func) => _dbProvider.TakeObject(func);
        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}