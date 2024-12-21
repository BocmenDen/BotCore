namespace OneBot.Extensions
{
    public static class SharedExtensions
    {
        public static T GetOrInstance<T>(this IDictionary<string, object> collection, string key, Func<T> create) where T : class
        {
            if (collection.TryGetValue(key, out object? value) && value is T castObject) return castObject!;
            castObject = create();
            collection[key] = castObject;
            return castObject;
        }
        public static T? GetOrDefault<T>(this IDictionary<string, object> collection, string key) where T : class
        {
            if (collection.TryGetValue(key, out object? value) && value is T castObject) return castObject!;
            return default;
        }
    }
}
