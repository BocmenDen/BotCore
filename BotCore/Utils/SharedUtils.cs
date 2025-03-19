using System.Reflection;

namespace BotCore.Utils
{
    public static class SharedUtils
    {
        public static int CalculateID<T>(params object[] secret) => HashCode.Combine(secret, typeof(T));

        public static bool TryGetInterfaceMethod(this Type @class, Type @interface, string methodName, out MethodInfo? method)
        {
            method = null;
            if (!@class.IsClass || !@interface.IsInterface) return false;
            if (!@interface.IsAssignableFrom(@class))
                return false;
            var map = @class.GetInterfaceMap(@interface);
            int index = Array.FindIndex(map.InterfaceMethods, m => m.Name == methodName);
            if(index < 0) return false;
            method = map.TargetMethods[index];
            return true;
        }

        public static MethodInfo GetInterfaceMethod(this Type @class, Type @interface, string methodName)
        {
            if (@class.TryGetInterfaceMethod(@interface, methodName, out var method))
                return method!;
            throw new Exception($"Не удалось получить метод {methodName} интерфейса {@interface} используемого в типе {@class}");
        }
    }
}
