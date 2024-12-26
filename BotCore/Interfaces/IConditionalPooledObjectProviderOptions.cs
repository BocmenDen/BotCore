namespace BotCore.Interfaces
{
    public interface IConditionalPooledObjectProviderOptions<T>
        where T : notnull
    {
        public int MaximumRetained { get; }
    }
}
