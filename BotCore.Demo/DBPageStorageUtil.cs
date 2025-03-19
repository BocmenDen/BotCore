using BotCore.Attributes;
using BotCore.PageRouter.Interfaces;
using BotCore.Services;

namespace BotCore.Demo
{
    /// <summary>
    /// TODO реализовать в качестве примера
    /// </summary>
    [Service(ServiceType.Singleton, typeof(IDBUserPageKey<User, string>))]
    public class DBPageStorageUtil(ConditionalPooledObjectProvider<DataBase> db) : IDBUserPageKey<User, string>,
                                                                                   IDBUserPageModel<User>,
                                                                                   IStorageProvider,
                                                                                   IDBUserPageParameter<User>
    {
        private readonly ConditionalPooledObjectProvider<DataBase> _db = db;

        public string? GetKeyPage(User user) => user.KeyPage;

        PageRouter.Models.StorageModel<T> IDBUserPageModel<User>.GetModel<T>(User user)
        {
            throw new NotImplementedException();
        }

        object? IDBUserPageParameter<User>.GetParameter(User user)
        {
            throw new NotImplementedException();
        }

        Task IStorageProvider.Save<T>(T model)
        {
            throw new NotImplementedException();
        }

        Task IDBUserPageKey<User, string>.SetKeyPage(User user, string? key)
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

        Task IDBUserPageParameter<User>.SetParameter(User user, object? parameter)
        {
            throw new NotImplementedException();
        }
    }
}