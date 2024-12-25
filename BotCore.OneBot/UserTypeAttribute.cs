namespace BotCore.OneBot
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class UserTypeAttribute(string typeName): Attribute
    {
        public string TypeName=typeName;
    }
}
