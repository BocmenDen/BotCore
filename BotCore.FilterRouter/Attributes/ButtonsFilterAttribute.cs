using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.FilterRouter.Utils;
using BotCore.Interfaces;
using BotCore.Models;
using System.Linq.Expressions;

namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ButtonsFilterAttribute<TUser>(string resourceKey, int row = -1, int column = -1) : BaseFilterAttribute<TUser>(true)
    where TUser : IUser
    {
        private readonly string _resourceKey = resourceKey;
        private readonly int? _row = row < 0 ? null : row;
        private readonly int? _column = column < 0 ? null : column;

        public override Expression GetExpression(WriterExpression<TUser> writerExpression)
        {
            var buttons = GetButtons(writerExpression);
            var resultSearch = GetSearchResult(writerExpression, buttons);
            var conditionFlag = GetConditionFlag(resultSearch);
            var result = writerExpression.CreateFilterResultParametrStructAutoKey<ButtonSearch, TUser>(resultSearch, conditionFlag, $"ResultButtonsFilter_{_resourceKey}_{_row}_{_column}");

            return result;
        }

        private BinaryExpression GetConditionFlag(ParameterExpression resultSearch)
        {
            BinaryExpression flag = Expression.Equal(resultSearch, Expression.Constant(null, typeof(Nullable<ButtonSearch>)));
            if (_row != null)
            {
                ConstantExpression rowConst = Expression.Constant(_row, typeof(int));
                MemberExpression rowMember = Expression.Field(Expression.Property(resultSearch, nameof(Nullable<ButtonSearch>.Value)), nameof(ButtonSearch.Row));
                flag = Expression.OrElse(flag, Expression.NotEqual(rowConst, rowMember));
            }
            if (_column != null)
            {
                ConstantExpression columnConst = Expression.Constant(_column, typeof(int));
                MemberExpression columnMember = Expression.Field(Expression.Property(resultSearch, nameof(Nullable<ButtonSearch>.Value)), nameof(ButtonSearch.Column));
                flag = Expression.OrElse(flag, Expression.NotEqual(columnConst, columnMember));
            }
            return flag;
        }

        private ParameterExpression GetSearchResult(WriterExpression<TUser> writerExpression, ConstantExpression buttons)
        {
            MethodCallExpression searchIndexButton = Expression.Call
                (
                    writerExpression.GetBotFunctionsParametr(),
                    typeof(IClientBotFunctions).GetMethod(nameof(IClientBotFunctions.GetIndexButton)) ?? throw new Exception("Method not found"),
                    writerExpression.GetUpdateParametr(),
                    buttons
                );
            ParameterExpression resultSearchVarable = Expression.Variable(typeof(ButtonSearch?), $"resultSearch_{_resourceKey}");
            var stateChache = writerExpression.ChacheOrGetExpressionAutoKey(ref resultSearchVarable, _resourceKey);
            if (stateChache == StateCache.Cached)
            {
                BinaryExpression assignResultSearch = Expression.Assign(resultSearchVarable, searchIndexButton);
                writerExpression.WriteBody(assignResultSearch);
            }
            return resultSearchVarable;
        }

        private ConstantExpression GetButtons(WriterExpression<TUser> writerExpression)
        {
            ButtonsSend buttons = (ResourceKeyUtil.GetValue<ButtonsSend>(_resourceKey) ??
                GetCondition(ResourceKeyUtil.GetValue<IEnumerable<IEnumerable<string>>>(_resourceKey)) ??
                GetCondition(ResourceKeyUtil.GetValue<IEnumerable<IEnumerable<ButtonSend>>>(_resourceKey))) ??
                throw new InvalidOperationException("ResourceKey not found");
            ConstantExpression buttonsEx = Expression.Constant(buttons);
            _ = writerExpression.ChacheOrGetExpressionAutoKey(ref buttonsEx, _resourceKey);
            return buttonsEx;
        }

        private static ButtonsSend? GetCondition(IEnumerable<IEnumerable<string>>? buttons)
        {
            if (buttons == null) return null;
            return new ButtonsSend(buttons.Select(x => x.Select(y => new ButtonSend(y))));
        }

        private static ButtonsSend? GetCondition(IEnumerable<IEnumerable<ButtonSend>>? buttons)
        {
            if (buttons == null) return null;
            return new ButtonsSend(buttons);
        }
    }
}