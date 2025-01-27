namespace BotCore.Interfaces
{
    public interface IDBClientParameterConverter<TParameter>
    {
        public long ParameterConvert(TParameter parameter);
    }
}
