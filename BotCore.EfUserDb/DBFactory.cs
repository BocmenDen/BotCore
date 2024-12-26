using BotCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BotCore.EfUserDb
{
    internal class DBFactory<DB>(IDbContextFactory<DB> originalFactory) : IFactory<DB>
        where DB : DbContext
    {
        public DB Create() => originalFactory.CreateDbContext();
    }
}
