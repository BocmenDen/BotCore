using BotCore.Attributes;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BotCore.Services
{
    [Service(ServiceType.Transient)]
    public class SingletonObjectProvider<TObject> : IObjectProvider<TObject>, IDisposable
        where TObject : notnull
    {
        public readonly TObject Object;

        public SingletonObjectProvider(IServiceProvider serviceProvider, IFactory<TObject>? factory = null)
        {
            if (factory != null)
                Object = factory.Create();
            else
                Object = serviceProvider.GetRequiredService<TObject>();
        }

        public void TakeObject(Action<TObject> func) => func(Object);
        public T TakeObject<T>(Func<TObject, T> func) => func(Object);

        public void Dispose()
        {
            if (Object is IDisposable disposable)
                disposable.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
