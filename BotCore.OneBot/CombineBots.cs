using BotCore.Attributes;
using BotCore.Interfaces;
using BotCore.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BotCore.OneBot
{
    [Service(ServiceType.Singltone)]
    public class CombineBots<TDB, TUser>(TypeNameGenerator typeNameGenerator,
                                         DBClientHelper<TUser, TDB, UserLinkInfo,
                                         ConditionalPooledObjectProvider<TDB>> database,
                                         ILogger<CombineBots<TDB, TUser>>? logger = null) : INextLayer<TUser, UpdateContextOneBot<TUser>>,
                                                                                            IInputLayer<IUser, IUpdateContext<IUser>>
        where TUser : IUser
        where TDB : class, IDB
    {
        private readonly ConcurrentDictionary<Type, string> _typesUser = [];

        private readonly ILogger<CombineBots<TDB, TUser>>? _logger = logger;
        private readonly TypeNameGenerator _typeNameGenerator = typeNameGenerator;
        private readonly DBClientHelper<TUser, TDB, UserLinkInfo, ConditionalPooledObjectProvider<TDB>> _database = database;

        public event Func<UpdateContextOneBot<TUser>, Task>? Update;

        public async Task HandleNewUpdateContext(IUpdateContext<IUser> context)
        {
            if (Update == null) return;
            Type type = context.User.GetType();
            string typeName;
            if (!_typesUser.TryGetValue(type, out typeName!))
            {
                typeName = _typeNameGenerator.GenerateId(type);
                _typesUser.TryAdd(type, typeName);
            }
            UserLinkInfo userLinkInfo = new(typeName, context.User.Id);
            var (user, isCreate) = await _database.GetOrCreate(userLinkInfo);
            if (isCreate)
                _logger?.LogInformation("Создан объединяющий пользователь {userNew}, на основе {userOld}", user, context.User);

            UpdateContextOneBot<TUser> newContext = new(context, user);
            await Update.Invoke(newContext);
        }
    }
}
