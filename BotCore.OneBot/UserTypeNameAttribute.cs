namespace BotCore.OneBot
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class UserTypeNameAttribute(string typeName) : Attribute
    {
        public string TypeName = typeName;
    }
}
