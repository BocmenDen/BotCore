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

        private readonly Func<TParameter, ValueTask<TUser>> _createUser;
        private readonly Func<TParameter, ValueTask<TUser?>> _getUser;
        private readonly Func<TParameter, ValueTask<(TUser user, bool isCreate)>> _getOrCreate;
        private readonly Action? _disponse;

        public DBClientHelper(TDBProvider dbProvider, IDBClientParameterConverter<TParameter>? converter = null)
        {
            _dbProvider = dbProvider;
            if (dbProvider is IDisposable disposable) _disponse = () => disposable.Dispose();
            if (dbProvider is IObjectProvider<IDBUser<TUser, TParameter>> castParameterDB)
            {
                _createUser = (p) => castParameterDB.TakeObjectAsync((db) => db.CreateUser(p));
                _getUser = (p) => castParameterDB.TakeObjectAsync((db) => db.GetUser(p));

                async ValueTask<(TUser user, bool isCreate)> getOrCreate(IDBUser<TUser, TParameter> db, TParameter p)
                {
                    var user = await db.GetUser(p);
                    bool isCreate = user == null;
                    return (user ?? await db.CreateUser(p), isCreate);
                }

                _getOrCreate = (p) => castParameterDB.TakeObjectAsync((db) => getOrCreate(db, p));
                return;
            }
            else if (dbProvider is IObjectProvider<IDBUser<TUser, long>> castDefaultDB && converter != null)
            {
                _createUser = (p) => castDefaultDB.TakeObjectAsync((db) => db.CreateUser(converter.ParameterConvert(p)));
                _getUser = (p) => castDefaultDB.TakeObjectAsync((db) => db.GetUser(converter.ParameterConvert(p)));

                async ValueTask<(TUser user, bool isCreate)> getOrCreate(IDBUser<TUser, long> db, TParameter p)
                {
                    var id = converter.ParameterConvert(p);
                    var user = await db.GetUser(id);
                    bool isCreate = user == null;
                    return (user ?? await db.CreateUser(id), isCreate);
                }
                _getOrCreate = (p) => castDefaultDB.TakeObjectAsync((db) => getOrCreate(db, p));
                return;
            }
            throw new Exception("Не удалось найти подходящую БД");
        }

        public ValueTask<TUser?> GetUser(TParameter parameter) => _getUser(parameter);
        public ValueTask<TUser> CreateUser(TParameter parameter) => _createUser(parameter);
        public ValueTask<(TUser user, bool isCreate)> GetOrCreate(TParameter parameter) => _getOrCreate(parameter);

        public void TakeObject(Action<TDB> func) => _dbProvider.TakeObject(func);
        public T TakeObject<T>(Func<TDB, T> func) => _dbProvider.TakeObject(func);
        public Task<T> TakeObjectAsync<T>(Func<TDB, Task<T>> func) => _dbProvider.TakeObjectAsync(func);
        public ValueTask<T> TakeObjectAsync<T>(Func<TDB, ValueTask<T>> func) => _dbProvider.TakeObjectAsync(func);
        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}