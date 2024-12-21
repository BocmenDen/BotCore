namespace OneBot.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceRegisterProvider(string serviceName) : Attribute
    {
        public string ServiceName = string.IsNullOrWhiteSpace(serviceName) ? throw new ArgumentException(nameof(serviceName)) : serviceName;
    }
}
