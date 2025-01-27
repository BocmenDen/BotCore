using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace BotCore.FilterRouter.Attributes
{
    public class RegexFilterAttribute<TUser>([StringSyntax("Regex", "options")] string pattern, RegexOptions options = RegexOptions.None, ReturnType returnType = ReturnType.NoReturn) : BaseFilterAttribute<TUser>(returnType != ReturnType.NoReturn)
        where TUser : IUser
    {
        public readonly Regex Regex = new(pattern, options);

        public override Expression GetExpression(WriterExpression<TUser> writerExpression)
        {
            string key = $"{pattern}_{options}_{returnType}";
            var regex = Expression.Constant(Regex, typeof(Regex));
            var varableRegex = Expression.Variable(typeof(Regex), $"regex_{key}");
            if (writerExpression.CacheOrGetExpressionAutoKey(ref varableRegex, key) == StateCache.Cached)
                writerExpression.WriteBody(Expression.Assign(varableRegex, regex));

            var message = writerExpression.GetMessageParameter();
            Expression condition = Expression.Equal(message, Expression.Constant(null, typeof(string)));
            var isMatch = Expression.Not(Expression.Call(varableRegex, typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string)])!, message));
            condition = Expression.OrElse(condition, isMatch);
            writerExpression.CacheOrGetExpressionAutoKey(ref condition, key);
            return returnType switch
            {
                ReturnType.NoReturn => condition,
                ReturnType.Match => writerExpression.CreateFilterResultParameterClassAutoKey<Match, TUser>(
                                        Expression.Call(varableRegex, typeof(Regex).GetMethod(nameof(Regex.Match), [typeof(string)])!, message),
                                        condition,
                                        key),
                ReturnType.Matches => writerExpression.CreateFilterResultParameterClassAutoKey<MatchCollection, TUser>(
                                        Expression.Call(varableRegex, typeof(Regex).GetMethod(nameof(Regex.Matches), [typeof(string)])!, message),
                                        condition,
                                        key),
                _ => throw new Exception($"Неизвестный тип {nameof(ReturnType)}"),
            };
        }
    }

    public enum ReturnType
    {
        NoReturn,
        Match,
        Matches
    }
}
