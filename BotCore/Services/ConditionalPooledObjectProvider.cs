using BotCore.Attributes;
using BotCore.Interfaces;
using BotCore.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Threading.Tasks.Sources;

namespace BotCore.Services
{
    [Service(ServiceType.Singleton)]
    public class ConditionalPooledObjectProvider<TObject> : IDisposable, IObjectProvider<TObject>
        where TObject : class
    {
        private readonly Func<TObject> _get;
        private readonly Action<TObject> _return;
        private readonly Action? _disponse;

        private readonly Action<Action<TObject>> _takeObjectNoRet;

        public ConditionalPooledObjectProvider(
            IServiceProvider serviceProvider,
            IFactory<TObject>? factory = null,
            IOptions<PooledObjectProviderOptions<TObject>>? conditionalPooledObjectProviderOptions = null,
            IReset<TObject>? reset = null
            )
        {
            if (factory == null)
            {
                _get = serviceProvider.GetRequiredService<TObject>;
                _takeObjectNoRet = (f) => f(_get());
                _return = (_) => { };
                return;
            }
            ObjectPool<TObject> pool = new DefaultObjectPoolProvider()
            {
                MaximumRetained = conditionalPooledObjectProviderOptions?.Value.MaximumRetained ?? Environment.ProcessorCount * 2
            }.Create(new PooledObjectPolicyDefault<TObject>(factory.Create, reset == null ? null : reset.Clear));

            if (pool is IDisposable disposable)
                _disponse = disposable.Dispose;

            _get = pool.Get;
            _return = pool.Return;
            _takeObjectNoRet = (a) =>
            {
                TObject obj = pool.Get();
                a(obj);
                pool.Return(obj);
            };
        }

        public TObject Get() => _get();

        public void Return(TObject @object) => _return(@object);

        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }

        public void TakeObject(Action<TObject> func) => _takeObjectNoRet(func);

        public T TakeObject<T>(Func<TObject, T> func)
        {
            TObject obj = Get();
            var value = func.Invoke(obj);
            Return(obj);
            return value;
        }
        public async Task<T> TakeObjectAsync<T>(Func<TObject, Task<T>> func)
        {
            TObject obj = Get();
            var value = await func.Invoke(obj);
            Return(obj);
            return value;
        }
        public async ValueTask<T> TakeObjectAsync<T>(Func<TObject, ValueTask<T>> func)
        {
            TObject obj = Get();
            var value = await func.Invoke(obj);
            Return(obj);
            return value;
        }
    }

    internal class PooledObjectPolicyDefault<T>(Func<T> create, Action<T>? @return) : IPooledObjectPolicy<T>
        where T : notnull
    {
        private readonly Func<T> _create = create??throw new ArgumentNullException(nameof(create));
        private readonly Action<T>? _return = @return;

        public T Create() => _create();

        public bool Return(T obj)
        {
            _return?.Invoke(obj);
            return true;
        }
    }
}
