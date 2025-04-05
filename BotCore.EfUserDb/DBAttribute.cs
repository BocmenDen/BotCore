using BotCore.Attributes;

namespace BotCore.EfDb
{
    public class DBAttribute() : ServiceAttribute(DBRegistrationProvaderName)
    {
        internal const string DBRegistrationProvaderName = "EFDatabase";
    }
}
