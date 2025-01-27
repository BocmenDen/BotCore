using BotCore.Attributes;
using BotCore.Interfaces;

namespace BotCore.Services
{
    [Service(ServiceType.Transient)]
    public class DBClientHelper<TUser, TDB, TParameter, TDBProvider> : IDBUser<TUser, TParameter>, IObjectProvider<TDB>
        where TUser : IUser
        where TDB : IDB
        where TDBProvider : IObjectProvider<TDB>
    {
        private readonly TDBProvider _dbProvider;

        private readonly Func<TParameter, Task<TUser>> _createUser;
        private readonly Func<TParameter, Task<TUser?>> _getUser;
        private readonly Func<TParameter, Task<(TUser user, bool isCreate)>> _getOrCreate;
        private readonly Action? _disponse;

        public DBClientHelper(TDBProvider dbProvider, IDBClientParameterConverter<TParameter>? converter = null)
        {
            _dbProvider = dbProvider;
            if (dbProvider is IDisposable disposable) _disponse = () => disposable.Dispose();
            if (dbProvider is IObjectProvider<IDBUser<TUser, TParameter>> castParametrDB)
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
                _createUser = (p) => castDefaultDB.TakeObject((db) => db.CreateUser(converter.ParameterConvert(p)));
                _getUser = (p) => castDefaultDB.TakeObject((db) => db.GetUser(converter.ParameterConvert(p)));
                _getOrCreate = (p) => castDefaultDB.TakeObject(async (db) =>
                {
                    var id = converter.ParameterConvert(p);
                    var user = await db.GetUser(id);
                    bool isCreate = user == null;
                    return (user ?? await db.CreateUser(id), isCreate);
                });
                return;
            }
            throw new Exception("Не удалось найти подходящую БД");
        }

        public Task<TUser?> GetUser(TParameter parameter) => _getUser(parameter);
        public Task<TUser> CreateUser(TParameter parameter) => _createUser(parameter);
        public Task<(TUser user, bool isCreate)> GetOrCreate(TParameter parameter) => _getOrCreate(parameter);

        public void TakeObject(Action<TDB> func) => _dbProvider.TakeObject(func);
        public T TakeObject<T>(Func<TDB, T> func) => _dbProvider.TakeObject(func);
        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}