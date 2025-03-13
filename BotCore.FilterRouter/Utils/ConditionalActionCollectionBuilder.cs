using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Reflection;

namespace BotCore.FilterRouter.Utils
{
    public class ConditionalActionCollectionBuilder<TUser, TContext>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        private readonly SortedList<int, List<Func<IServiceProvider, TContext, EvaluatedAction>>> _actions = [];

        private ConditionalActionCollectionBuilder() { }

        public static ConditionalActionCollectionBuilder<TUser, TContext> Create() => new();
        public static ConditionalActionCollectionBuilder<TUser, TContext> CreateAutoDetectFromCurrentDomain(ILogger? logger = null)
            => (new ConditionalActionCollectionBuilder<TUser, TContext>()).LoadFromAssemblies(logger, AppDomain.CurrentDomain.GetAssemblies());

        public ConditionalActionCollectionBuilder<TUser, TContext> Add(Func<IServiceProvider, TContext, EvaluatedAction> action, int priority = -1)
        {
            if (priority < 0) priority = -1;
            if (!_actions.TryGetValue(priority, out var actionCollection))
            {
                actionCollection = [];
                _actions.Add(priority, actionCollection);
            }
            actionCollection.Add(action);
            return this;
        }

        public ConditionalActionCollectionBuilder<TUser, TContext> LoadFromAssemblies(ILogger? logger, params Assembly?[] assemblies)
            => LoadFromAssemblies(logger, assemblies as IEnumerable<Assembly?>);

        public ConditionalActionCollectionBuilder<TUser, TContext> LoadFromAssemblies(ILogger? logger, IEnumerable<Assembly?> assemblies)
        {
            if (assemblies == null) return this;
            foreach (var assembly in assemblies)
                LoadFromAssembly(logger, assembly);
            return this;
        }

        public ConditionalActionCollectionBuilder<TUser, TContext> LoadFromAssembly(ILogger? logger, Assembly? assembly)
        {
            if (assembly is null) return this;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    try
                    {
                        var priorityInfo = method.GetCustomAttribute<FilterPriorityAttribute>();
                        int priority = -1;
                        if (priorityInfo != null) priority = priorityInfo.Priority;
                        if (priorityInfo == null && !method.GetCustomAttributes<BaseFilterAttribute<TUser>>().Any()) continue;
                        Add(BuilderFilters.CompileFilters<TUser, TContext>(logger, method), priority);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "Ну удалось зарегистрировать метод [{methodName}] обработки сообщений в {typeName}", method.Name, type.FullName);
                    }
                }
            }
            return this;
        }

        public IConditionalActionCollection<TUser, TContext> Build() => new ConditionalActionCollectionDefault([.. _actions.Values.SelectMany(x => x)]);

        private class ConditionalActionCollectionDefault(Func<IServiceProvider, TContext, EvaluatedAction>[] actions) : IConditionalActionCollection<TUser, TContext>
        {
            private readonly Func<IServiceProvider, TContext, EvaluatedAction>[] _actions = actions;

            public IEnumerator<Func<IServiceProvider, TContext, EvaluatedAction>> GetEnumerator() => _actions.AsEnumerable().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _actions.GetEnumerator();
        }
    }
}
