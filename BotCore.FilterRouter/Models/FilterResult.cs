namespace BotCore.FilterRouter.Models
{
    public readonly struct FilterResult<T>(bool isSuccess, T? value = default)
    {
        public readonly bool IsSuccess = isSuccess;
        public readonly T? Value = value;
    }
}