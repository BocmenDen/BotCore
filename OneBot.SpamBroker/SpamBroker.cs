using Microsoft.Extensions.Logging;
using OneBot.Interfaces;
using System.Collections.Concurrent;

namespace OneBot.SpamBroker
{
    public class SpamBroker<TValue, TUser>(
            Func<IUpdateContext<TUser>, TValue> getValue,
            int maxEvent, TimeSpan timeWindow,
            ILogger<SpamBroker<TValue, TUser>>? logger
        ) : ISpam<TUser>
        where TUser : IUser
        where TValue : notnull
    {
        public int MaxEvent => maxEvent;
        public TimeSpan TimeWindow => timeWindow;

        private readonly ConcurrentDictionary<TValue, ConcurrentQueue<DateTime>> _eventHistory = new();

        private void CheckInit()
        {
            ArgumentNullException.ThrowIfNull(getValue);
        }

        public StateSpam GetSpamState(IUpdateContext<TUser> context)
        {
            CheckInit();
            var identifier = getValue!(context);
            DateTime now = DateTime.UtcNow;

            var events = _eventHistory.GetOrAdd(identifier, _ => new ConcurrentQueue<DateTime>());
            ClearHistoryForKey(events, now);
            events.Enqueue(now);
            if (events.Count > maxEvent)
            {
                logger?.LogWarning(Consts.EventIdForbidden, Consts.LogForbidden, context.User);
                return StateSpam.Forbidden;
            }
            else if (events.Count >= maxEvent)
            {
                logger?.LogWarning(Consts.EventIdForbiddenFirst, Consts.LogForbiddenFirst, context.User);
                return StateSpam.ForbiddenFirst;
            }
            return StateSpam.Allowed;
        }

        public int CleanupEmptyHistory() => CleanupEmptyHistory(out int _);
        public int CleanupEmptyHistory(out int countAll)
        {
            CheckInit();
            int countUsers = 0;
            countAll = 0;
            DateTime now = DateTime.UtcNow;
            foreach (var key in _eventHistory.Keys.ToArray())
            {
                if (_eventHistory.TryGetValue(key, out var events))
                {
                    ClearHistoryForKey(events, now);
                    if (events.IsEmpty)
                    {
                        _eventHistory.TryRemove(key, out _);
                        countUsers++;
                        logger?.LogInformation("История пользователя с ключом {key} была удалена для экономии памяти", key);
                    }
                    countAll += events.Count;
                }
            }
            return countUsers;
        }
        private void ClearHistoryForKey(ConcurrentQueue<DateTime> hostory, DateTime now)
        {
            while (hostory.TryPeek(out DateTime timestamp) && (now - timestamp) > timeWindow)
                hostory.TryDequeue(out _);
        }
    }
}
