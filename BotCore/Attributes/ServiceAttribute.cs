namespace BotCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute(string type) : Attribute
    {
        public readonly string LifetimeType = string.IsNullOrWhiteSpace(type) ? throw new ArgumentException(nameof(type)) : type;
        public ServiceAttribute(ServiceType serviceType) : this(serviceType.ToString()) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute<T> : ServiceAttribute
    {
        public ServiceAttribute(ServiceType serviceType) : base(serviceType) { }
        public ServiceAttribute(string type) : base(type) { }
    }

    public enum ServiceType
    {
        Singltone,
        Scoped,
        Transient
    }
}
