namespace OneBot.Interfaces
{
    public interface IFactory<DB>
    {
        public DB Create();
    }
}
