using BotCore.FilterRouter.Attributes;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Collections;
using System.Reflection;

namespace BotCore.FilterRouter.Utils
{
    public class ConditionalActionCollectionBuilder<TUser> where TUser : IUser
    {
        private readonly SortedList<int, List<Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>>> _actions = [];

        private ConditionalActionCollectionBuilder() { }

        public static ConditionalActionCollectionBuilder<TUser> Create() => new();
        public static ConditionalActionCollectionBuilder<TUser> CreateAutoDetectFromCurrentDomain()
            => (new ConditionalActionCollectionBuilder<TUser>()).LoadFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

        public ConditionalActionCollectionBuilder<TUser> Add(Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction> action, int priority = -1)
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

        public ConditionalActionCollectionBuilder<TUser> LoadFromAssemblies(params Assembly?[] assemblies)
            => LoadFromAssemblies(assemblies as IEnumerable<Assembly?>);

        public ConditionalActionCollectionBuilder<TUser> LoadFromAssemblies(IEnumerable<Assembly?> assemblies)
        {
            if (assemblies == null) return this;
            foreach (var assembly in assemblies)
                LoadFromAssembly(assembly);
            return this;
        }

        public ConditionalActionCollectionBuilder<TUser> LoadFromAssembly(Assembly? assembly)
        {
            if (assembly is null) return this;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var priorityInfo = method.GetCustomAttribute<FilterPriorityAttribute>();
                    int priority = -1;
                    if (priorityInfo != null) priority = priorityInfo.Priority;
                    if (priorityInfo == null && !method.GetCustomAttributes<BaseFilterAttribute<TUser>>().Any()) continue;
                    Add(BuilderFilters.CompileFilters<TUser>(method), priority);
                }
            }
            return this;
        }

        public IConditionalActionCollection<TUser> Build() => new ConditionalActionCollectionDefault(_actions.Values.SelectMany(x => x).ToArray());

        private class ConditionalActionCollectionDefault(Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>[] actions) : IConditionalActionCollection<TUser>
        {
            private readonly Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>[] _actions = actions;

            public IEnumerator<Func<IServiceProvider, IUpdateContext<TUser>, EvaluatedAction>> GetEnumerator() => _actions.AsEnumerable().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _actions.GetEnumerator();
        }
    }
}
