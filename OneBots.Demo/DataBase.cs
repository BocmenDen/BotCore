using Microsoft.EntityFrameworkCore;
using OneBot.EfUserDb;
using OneBot.Interfaces;

namespace OneBots.Demo
{
    [DB]
    public class DataBase(DbContextOptions options) : DbContext(options), IDBUser<User, long>
    {
        public DbSet<User> Users { get; set; } = null!;

        public async Task<User> CreateUser(long parameter)
        {
            User user = new() { Id = parameter };
            var e = Users.Add(user);
            await SaveChangesAsync();
            e.State = EntityState.Detached;
            return user;
        }

        public Task<User?> GetUser(long parameter) => Users.FindAsync(parameter).AsTask();
    }
}
