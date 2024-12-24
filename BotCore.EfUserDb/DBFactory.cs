using Microsoft.EntityFrameworkCore;
using BotCore.Attributes;
using BotCore.Interfaces;

namespace BotCore.EfUserDb
{
    [Service(ServiceType.Singltone)]
    internal class DBFactory<DB>(IDbContextFactory<DB> originalFactory) : IFactory<DB>
        where DB : DbContext
    {
        public DB Create() => originalFactory.CreateDbContext();
    }
}
