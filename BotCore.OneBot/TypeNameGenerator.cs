using BotCore.Attributes;
using System.Reflection;

namespace BotCore.OneBot
{
    [Service(ServiceType.Singltone)]
    public class TypeNameGenerator
    {
        private readonly Func<Type, string> _generator;

        public TypeNameGenerator(TypeNameGeneratorOptions? generatorOptions = null)
        {
            if (generatorOptions != null)
            {
                _generator = generatorOptions.Generator;
                return;
            }
            _generator = (type) =>
            {
                var attr = type.GetCustomAttribute<UserTypeNameAttribute>();
                if (attr != null)
                    return attr.TypeName;
                return type.Name;
            };
        }

        public string GenerateId(Type type) => _generator(type);
    }

    public class TypeNameGeneratorOptions(Func<Type, string> generator)
    {
        public readonly Func<Type, string> Generator = generator??throw new ArgumentNullException(nameof(generator));
    }
}
