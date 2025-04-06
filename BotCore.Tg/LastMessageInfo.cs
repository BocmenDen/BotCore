namespace BotCore.Tg
{
    public class LastMessageInfo
    {
        public DateTime OldestMessage { get; init; } = DateTime.Now;
        public int[]? MessagesMediaGroup { get; init; }
        public int[]? Messages { get; init; }

        public bool IsInline { get; init; }

        public IEnumerable<int> AllMessages => MessagesMediaGroup?.Concat(Messages ?? []) ?? Messages ?? [];
    }
}
