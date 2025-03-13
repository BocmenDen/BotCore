namespace BotCore.PageRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PageFactoryCompilerAttribute(string name) : Attribute
    {
        public readonly string Name = name;
    }
}