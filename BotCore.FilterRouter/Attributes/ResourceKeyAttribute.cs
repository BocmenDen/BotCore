namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public class ResourceKeyAttribute(string resourceKey) : Attribute
    {
        public readonly string ResourceKey = resourceKey;
    }
}
