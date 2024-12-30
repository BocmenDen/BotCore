namespace BotCore.FilterRouter.Models
{
    public readonly struct EvaluatedAction(bool isIgnore, Func<Task<bool>>? execute)
    {
        public readonly bool IsIgnore = isIgnore;
        public readonly Func<Task<bool>>? Execute = execute;
    }
}
