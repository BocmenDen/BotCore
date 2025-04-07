using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandFilterAttribute<TUser>(bool isIgnoreCase, params string[] commands) : BaseFilterAttribute<TUser>(false)
        where TUser : IUser
    {
        private readonly string[] _commands = isIgnoreCase ? [.. commands.Select(x => x.ToLower())] : commands;
        private readonly bool _isIgnoreCase = isIgnoreCase;

        public CommandFilterAttribute(params string[] commands) : this(false, commands) { }

        public override Expression GetExpression(WriterExpression<TUser> writerExpression)
        {
            MemberExpression originalCommand = writerExpression.GetUpdateCommand();
            Expression commandExpression = originalCommand;
            var commandsConstant = Expression.Constant(_commands.AsEnumerable());
            if (_isIgnoreCase)
            {
                commandExpression = Expression.Call(commandExpression, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes) ??
                    throw new Exception($"Метод [{nameof(String)}.{nameof(string.ToLower)}] не найден"));
            }

            MethodInfo containsMethodDefinition = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.Name == nameof(Enumerable.Contains) && x.GetParameters().Length == 2)
                .First()
                .MakeGenericMethod(typeof(string));

            MethodCallExpression containsMethod = Expression.Call(
                containsMethodDefinition,
                commandsConstant,
                commandExpression
            );
            return Expression.Condition(Expression.Equal(originalCommand, Expression.Constant(null, typeof(string))), Expression.Constant(true, typeof(bool)), Expression.Not(containsMethod));
        }
    }
}
