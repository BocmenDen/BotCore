using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Linq.Expressions;

namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class BaseFilterAttribute<TUser>(bool isReturnValue) : Attribute
        where TUser : IUser
    {
        public readonly bool IsReturnValue = isReturnValue;
        public abstract Expression GetExpression(WriterExpression<TUser> writerExpression);
    }
}