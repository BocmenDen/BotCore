using BotCore.Attributes;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace BotCore.Services
{
    [Service(ServiceType.Singltone)]
    public class ConditionalPooledObjectProvider<TObject> : IDisposable, IObjectProvider<TObject>
        where TObject : class
    {
        private readonly Func<TObject> _get;
        private readonly Action<TObject> _return;
        private readonly Action? _disponse;

        public ConditionalPooledObjectProvider(
            IServiceProvider serviceProvider,
            IFactory<TObject>? factory = null,
            IConditionalPooledObjectProviderOptions<TObject>? conditionalPooledObjectProviderOptions = null,
            IReset<TObject>? reset = null
            )
        {
            if (factory == null)
            {
                _get = () => serviceProvider.GetRequiredService<TObject>();
                _return = (_) => { };
                return;
            }
            Action<TObject>? clearF = null;
            if (reset != null)
                clearF = (v) => reset.Clear(v);
            ObjectPool<TObject> pool = new DefaultObjectPoolProvider()
            {
                MaximumRetained = conditionalPooledObjectProviderOptions?.MaximumRetained ?? Environment.ProcessorCount * 2
            }.Create(new PooledObjectPolicyDefault<TObject>(factory.Create, clearF));

            if (pool is IDisposable disposable)
                _disponse = disposable.Dispose;

            _get = pool.Get;
            _return = pool.Return;
        }

        public TObject Get() => _get();

        public void Return(TObject @object) => _return(@object);

        public void Dispose()
        {
            _disponse?.Invoke();
            GC.SuppressFinalize(this);
        }

        public void TakeObject(Action<TObject> func)
        {
            TObject obj = Get();
            func.Invoke(obj);
            Return(obj);
        }

        public T TakeObject<T>(Func<TObject, T> func)
        {
            TObject obj = Get();
            var value = func.Invoke(obj);
            if (value is Task task)
                task.ConfigureAwait(false).GetAwaiter().OnCompleted(() => Return(obj));
            else
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
