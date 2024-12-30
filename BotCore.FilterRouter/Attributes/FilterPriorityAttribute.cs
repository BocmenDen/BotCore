namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FilterPriorityAttribute(ushort priority) : Attribute
    {
        public readonly ushort Priority = priority;
    }
}
