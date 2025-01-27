using BotCore.Interfaces;
using System.Collections;
using System.Linq.Expressions;

namespace BotCore.FilterRouter.Models
{
    public class WriterExpression<TUser> : IEnumerable<Expression>
        where TUser : IUser
    {
        private readonly Dictionary<string, Expression> _expressions = [];
        private readonly List<Expression> _body = [];
        private readonly List<ParameterExpression> _parameterExpressions = [];
        public readonly ParameterExpression ContextParameter = Expression.Parameter(typeof(IUpdateContext<TUser>));
        public readonly ParameterExpression ServiceProvider = Expression.Parameter(typeof(IServiceProvider));

        public IEnumerable<ParameterExpression> GetParameterExpressions() => _parameterExpressions.Concat(_expressions.Values.OfType<ParameterExpression>());

        public IEnumerator<Expression> GetEnumerator() => ((IEnumerable<Expression>)_body).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_body).GetEnumerator();

        public bool TryAddCacheExpression(Expression expression, string key)
            => _expressions.TryAdd(key, expression);
        public bool TryGetCacheExpression(string key, out Expression? expression)
            => _expressions.TryGetValue(key, out expression);

        public void WriteBody(Expression expression) => _body.Add(expression);

        public void RegisterNoCacheParameter(ParameterExpression parameterExpression) => _parameterExpressions.Add(parameterExpression);
    }
}
