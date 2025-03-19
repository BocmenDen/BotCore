using Microsoft.Extensions.Caching.Memory;

namespace BotCore.PageRouter.Interfaces
{
    public interface IGetCacheOptions
    {
        public MemoryCacheEntryOptions GetCacheOptions();
    }
}
