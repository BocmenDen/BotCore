namespace BotCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute(string serviceType, Type? type = null) : Attribute
    {
        public readonly Type? Type = type;
        public readonly string LifetimeType = string.IsNullOrWhiteSpace(serviceType) ? throw new ArgumentException(nameof(serviceType)) : serviceType;
        public ServiceAttribute(ServiceType serviceType, Type? type = null) : this(serviceType.ToString(), type) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute<T> : ServiceAttribute
    {
        public ServiceAttribute(ServiceType serviceType) : base(serviceType, typeof(T)) { }
        public ServiceAttribute(string type) : base(type, typeof(T)) { }
    }

    public enum ServiceType
    {
        Singltone,
        Scoped,
        Transient,
        Hosted
    }
}
