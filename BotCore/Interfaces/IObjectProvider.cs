namespace BotCore.Interfaces
{
    public interface IObjectProvider<out TObject>
    {
        public void TakeObject(Action<TObject> func);
        public T TakeObject<T>(Func<TObject, T> func);
        public ValueTask<T> TakeObjectAsync<T>(Func<TObject, ValueTask<T>> func);
        public Task<T> TakeObjectAsync<T>(Func<TObject, Task<T>> func);
    }
}
