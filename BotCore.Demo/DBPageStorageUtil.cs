using BotCore.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.Services;

namespace BotCore.Demo
{
    [Service(ServiceType.Singleton, typeof(IDBUserPageKey<User, string>))]
    public class DBPageStorageUtil(ConditionalPooledObjectProvider<DataBase> db) : IDBUserPageKey<User, string>
    {
        private readonly ConditionalPooledObjectProvider<DataBase> _db = db;

        public string? GetKeyPage(User user) => user.KeyPage;

        public Task SetKeyPage(User user, string? key)
        {
            return _db.TakeObject(async (db) =>
            {
                var t = db.Users.Local;

                user.KeyPage = key;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            });
        }
    }
}