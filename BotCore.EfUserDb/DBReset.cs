using BotCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BotCore.EfDb
{
    public class DBReset<DB> : IReset<DB>
        where DB : DbContext
    {
        public void Clear(DB value) => value.ChangeTracker.Clear();
    }
}
