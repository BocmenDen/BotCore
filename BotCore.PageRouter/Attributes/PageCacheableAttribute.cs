namespace BotCore.PageRouter.Attributes
{
    public class PageCacheableAttribute<TKey>(TKey Key, int hour = -1, int minutes = -1, int second = -1) : PageAttribute<TKey>(Key, "") where TKey : notnull
    {
        public TimeSpan SlidingExpiration = (hour <= 0 && minutes <= 0 && second <= 0) ? TimeSpan.FromMinutes(1) : new TimeSpan(hour < 0 ? 0 : hour, minutes < 0 ? 0 : minutes, second < 0 ? 0 : second);
    }
}
