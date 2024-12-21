using Microsoft.EntityFrameworkCore;
using OneBot.Attributes;
using OneBot.Interfaces;

namespace OneBot.EfUserDb
{
    [Service(ServiceType.Singltone)]
    internal class DBFactory<DB>(IDbContextFactory<DB> originalFactory) : IFactory<DB>
        where DB : DbContext
    {
        public DB Create() => originalFactory.CreateDbContext();
    }
}
