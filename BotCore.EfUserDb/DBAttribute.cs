using BotCore.Attributes;

namespace BotCore.EfUserDb
{
    public class DBAttribute() : ServiceAttribute(DBRegistrationProvaderName)
    {
        internal const string DBRegistrationProvaderName = "EFDatabase";
    }
}
