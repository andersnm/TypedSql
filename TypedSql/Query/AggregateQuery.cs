using System;
using System.Linq.Expressions;

namespace TypedSql
{
    public abstract class AggregateQuery<TFrom, T> : Query<TFrom, T>
    {
        public AggregateQuery(Query parent)
            : base(parent)
        {
        }

        public AggregateQuery<TFrom, T> Having(Expression<Func<T, bool>> whereExpr)
        {
            return new HavingQuery<TFrom, T>(this, whereExpr);
        }
    }

    internal class AggregateVisitor : ExpressionVisitor
    {
        public bool CalledAggregateFunction { get; set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Function))
            {
                if (node.Method.Name == nameof(Function.Count) || node.Method.Name == nameof(Function.Sum))
                {
                    CalledAggregateFunction = true;
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
