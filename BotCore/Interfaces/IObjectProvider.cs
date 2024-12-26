namespace BotCore.Interfaces
{
    public interface IObjectProvider<out TObject>
    {
        public T TakeObject<T>(Func<TObject, T> func);
        public void TakeObject(Action<TObject> func);
    }
}
