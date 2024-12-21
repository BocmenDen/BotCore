namespace OneBot.Interfaces
{
    public interface IDBClientParametrConverter<TParametr>
    {
        public long ParametrConvert(TParametr parametr);
    }
}
