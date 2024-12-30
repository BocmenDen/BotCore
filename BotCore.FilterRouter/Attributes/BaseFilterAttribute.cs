using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Linq.Expressions;

namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class BaseFilterAttribute<TUser> : Attribute
        where TUser : IUser
    {
        public abstract ParameterExpression GetExpression(WriterExpression<TUser> writerExpression);
    }
}