namespace BotCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute(string serviceType, params Type[] types) : Attribute
    {
        public readonly Type[] Types = types;
        public readonly string LifetimeType = string.IsNullOrWhiteSpace(serviceType) ? throw new ArgumentException(nameof(serviceType)) : serviceType;
        public ServiceAttribute(ServiceType serviceType, params Type[] types) : this(serviceType.ToString(), types) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute<T> : ServiceAttribute
    {
        public ServiceAttribute(ServiceType serviceType) : base(serviceType, typeof(T)) { }
        public ServiceAttribute(string type) : base(type, typeof(T)) { }
    }

    public enum ServiceType
    {
        Singleton,
        Scoped,
        Transient,
        Hosted
    }
}
