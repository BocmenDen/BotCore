using BotCore.Attributes;
using BotCore.Interfaces;
using BotCore.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BotCore.OneBot
{
    [Service(ServiceType.Singltone)]
    public class CombineBots<TUser, TDB>(TypeNameGenerator typeNameGenerator, DBClientHelper<TUser, TDB, UserLinkInfo, ConditionalPooledObjectProvider<TDB>> database, ILogger<CombineBots<TUser, TDB>>? logger = null)
        where TUser : IUser
        where TDB : class, IDB
    {
        private readonly ConcurrentDictionary<Type, string> _typesUser = [];

        private readonly ILogger<CombineBots<TUser, TDB>>? _logger = logger;
        private readonly TypeNameGenerator _typeNameGenerator = typeNameGenerator;
        private readonly DBClientHelper<TUser, TDB, UserLinkInfo, ConditionalPooledObjectProvider<TDB>> _database = database;

        public event Action<IUpdateContext<TUser>>? NewUpdateContext;

        public async void HandleNewUpdateContext<T>(IUpdateContext<T> context) where T : IUser
        {
            Type type = typeof(T);
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

            UpdateContextOneBot<TUser, T> newContext = new(context, user);
            NewUpdateContext?.Invoke(newContext);
        }
    }
}
