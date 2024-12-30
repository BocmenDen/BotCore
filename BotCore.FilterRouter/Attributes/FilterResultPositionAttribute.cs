namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FilterResultPositionAttribute(ushort position) : Attribute
    {
        public readonly ushort Position = position;
    }
}
