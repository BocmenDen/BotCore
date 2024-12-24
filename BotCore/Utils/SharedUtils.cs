namespace BotCore.Utils
{
    public static class SharedUtils
    {
        public static int CalculeteID<T>(params object[] secret) => HashCode.Combine(secret, typeof(T));
    }
}
