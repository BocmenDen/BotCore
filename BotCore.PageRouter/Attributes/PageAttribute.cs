namespace BotCore.PageRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class PageAttribute<TKey>(TKey key, string compilerName = PageFactoryCompiler.DefaultCompiler) : Attribute
        where TKey : notnull
    {
        public readonly TKey Key = key;
        public readonly string CompilerName = compilerName;
    }
}