using OneBot.Attributes;

namespace OneBot.EfUserDb
{
    public class DBAttribute() : ServiceAttribute(DBRegistrationProvaderName)
    {
        internal const string DBRegistrationProvaderName = "EFDatabase";
    }
}
