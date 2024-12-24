using Microsoft.Extensions.Logging;
using BotCore.Interfaces;
using System.Collections.Concurrent;

namespace BotCore.SpamBroker
{
    public class SingleMessageQueue<TValue, TUser>(
            Func<IUpdateContext<TUser>, TValue> getValue,
            ILogger<SingleMessageQueue<TValue, TUser>>? logger
        ) : ISpam<TUser>
        where TUser : IUser
        where TValue : notnull
    {
        private readonly ConcurrentDictionary<TValue, bool> _eventHistory = new();

        public StateSpam GetSpamState(IUpdateContext<TUser> context)
        {
            var key = getValue(context);
            if (_eventHistory.TryGetValue(key, out bool isSend))
            {
                if (isSend)
                {
                    logger?.LogWarning(Consts.EventIdForbidden, Consts.LogForbidden, context.User);
                    return StateSpam.Forbidden;
                }
                else
                {
                    _eventHistory.TryUpdate(key, true, false);
                    logger?.LogInformation(Consts.EventIdForbiddenFirst, Consts.LogForbiddenFirst, context.User);
                    return StateSpam.ForbiddenFirst;
                }
            }
            return StateSpam.Allowed;
        }

        public void RegisterEvent(IUpdateContext<TUser> context)
        {
            _eventHistory.TryAdd(getValue(context), false);
        }

        public void UnregisterEvent(IUpdateContext<TUser> context)
        {
            _eventHistory.TryRemove(getValue(context), out bool _);
        }

        public Metric GetMetric()
        {
            int countAll = 0;
            int forbiddenSpam = 0;
            foreach (var key in _eventHistory.Keys.ToArray())
            {
                if (_eventHistory.TryGetValue(key, out bool state))
                {
                    if (state) forbiddenSpam++;
                    countAll++;
                }
            }
            return new Metric(countAll, forbiddenSpam);
        }

        public readonly struct Metric(int countAll, int countForbiddenSpam)
        {
            public readonly int CountAll = countAll;
            public readonly int CountForbiddenSpam = countForbiddenSpam;
            public readonly int CountForbidden => CountAll - CountForbiddenSpam;
        }
    }
}
