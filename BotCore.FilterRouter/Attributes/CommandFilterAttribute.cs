using BotCore.FilterRouter.Extensions;
using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace BotCore.FilterRouter.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandFilterAttribute<TUser>(bool isIgnoreCase, params string[] commands) : BaseFilterAttribute<TUser> where TUser : IUser
    {
        private readonly string[] _commands = isIgnoreCase ? commands.Select(x => x.ToLower()).ToArray() : commands;
        private readonly bool _isIgnoreCase;

        public CommandFilterAttribute(params string[] commands) : this(false, commands) { }

        public override ParameterExpression GetExpression(WriterExpression<TUser> writerExpression)
        {
            MemberExpression originalCommand = writerExpression.GetUpdateCommand<TUser>();
            Expression commandExpression = originalCommand;
            var commandsConstant = Expression.Constant(_commands.AsEnumerable());
            if (_isIgnoreCase)
            {
                ParameterExpression commandToLower = Expression.Parameter(typeof(string));
                MethodCallExpression callToLower = Expression.Call(commandExpression, typeof(string).GetMethod(nameof(string.ToLower)) ??
                    throw new Exception("Method [string.ToLower] not found"));
                BinaryExpression assignResultSearch = Expression.Assign(commandToLower, callToLower);
                writerExpression.WriteBody(assignResultSearch);
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

            ParameterExpression result = writerExpression.CreateFilterResultParametrClassAutoKey<string, TUser>(originalCommand, Expression.Not(containsMethod));
            return result;
        }
    }
}
