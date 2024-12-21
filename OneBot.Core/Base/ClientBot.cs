using OneBot.Interfaces;
using OneBot.Models;

namespace OneBot.Base
{
    public abstract class ClientBot<TUser> : IClientBotFunctions, IDisposable where TUser : IUser
    {
        public int Id { get; protected set; }

        public event Action<IUpdateContext<TUser>>? Update;

        protected async Task HandleUpdate(Func<Task<UpdateContext<TUser>?>> fUpdateContext)
        {
            if (Update == null) return;
            var update = await fUpdateContext();
            if (update == null) return;
            Update.Invoke(update);
        }

        public abstract Task Run(CancellationToken token = default);

        public abstract Task Send(TUser user, SendModel send, UpdateModel? reply = null);
        public abstract ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend);
        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
