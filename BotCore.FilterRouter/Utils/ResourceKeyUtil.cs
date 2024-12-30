using BotCore.FilterRouter.Attributes;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.FilterRouter.Utils
{
    public static class ResourceKeyUtil
    {
        private static readonly Dictionary<string, Func<object?>> _resources = [];

        static ResourceKeyUtil()
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()))
            {
                foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var attr = member.GetCustomAttribute<ResourceKeyAttribute>();
                    if (attr == null) continue;
                    switch (member)
                    {
                        case FieldInfo field when field.IsStatic:
                            _resources.Add(attr.ResourceKey, Expression.Lambda<Func<object>>(Expression.Field(null, field)).Compile());
                            break;
                        case PropertyInfo property when property.CanRead && property.GetMethod!.IsStatic == true:
                            _resources.Add(attr.ResourceKey, Expression.Lambda<Func<object>>(Expression.Property(null, property)).Compile());
                            break;
                    }
                }
            }
        }

        public static T? GetValue<T>(string key)
        {
            if (_resources.TryGetValue(key, out var func))
                return (T?)func();
            return default;
        }
    }
}
