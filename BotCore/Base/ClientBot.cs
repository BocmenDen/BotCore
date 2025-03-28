﻿using BotCore.Interfaces;
using BotCore.Models;
using Microsoft.Extensions.Hosting;

namespace BotCore.Base
{
    public abstract class ClientBot<TUser, TContext> : BackgroundService, IClientBot<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public int Id { get; protected set; }

        public event Func<TContext, Task>? Update;

        protected async Task HandleUpdate(Func<Task<TContext?>> fUpdateContext)
        {
            if (Update == null) return;
            var update = await fUpdateContext();
            if (update == null) return;
            await Update.Invoke(update);
        }

        public abstract Task Send(TUser user, SendModel send, UpdateModel? reply = null);
        public abstract ButtonSearch? GetIndexButton(UpdateModel update, ButtonsSend buttonsSend);
    }
}
