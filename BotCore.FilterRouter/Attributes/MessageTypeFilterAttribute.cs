using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using BotCore.Models;
using System.Linq.Expressions;

namespace BotCore.FilterRouter.Attributes
{
    public class MessageTypeFilterAttribute<TUser>(UpdateType updateType) : BaseFilterAttribute<TUser>(false)
        where TUser : IUser
    {
        public readonly UpdateType UpdateType = updateType;

        public override Expression GetExpression(WriterExpression<TUser> writerExpression)
        {
            Expression updateType = writerExpression.GetUpdateType();
            Expression constant = Expression.Constant(UpdateType, typeof(Enum));
            var method = Expression.Call(updateType, typeof(UpdateType).GetMethod(nameof(UpdateType.HasFlag)) ?? throw new Exception(), constant);
            return Expression.Not(method);
        }
    }
}
