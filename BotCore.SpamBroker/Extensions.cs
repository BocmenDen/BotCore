using BotCore.Interfaces;
using BotCore.Models;

namespace BotCore.SpamBroker
{
    public static class Extensions
    {
        public static bool IsSpam(this StateSpam stateSpam) => stateSpam == StateSpam.Forbidden || stateSpam == StateSpam.ForbiddenFirst;
        public static async Task<StateSpam> CheckMessageSpamStatus<TUser>(this ISpam<TUser> filter, IUpdateContext<TUser> context, SendModel messageFirstSpam) where TUser : IUser
        {
            var state = filter.GetSpamState(context);
            if (state == StateSpam.ForbiddenFirst)
                await context.Reply(messageFirstSpam);
            return state;
        }
        public static async Task<bool> IsSpan<TUser>(this ISpam<TUser> filter, IUpdateContext<TUser> context, SendModel messageFirstSpam) where TUser : IUser
            => (await filter.CheckMessageSpamStatus(context, messageFirstSpam)).IsSpam();
    }
}
