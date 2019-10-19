using System;
using System.Linq.Expressions;

namespace TypedSql
{
    /// <summary>
    /// Helper class for building query predicates
    /// Similar to http://www.albahari.com/nutshell/predicatebuilder.aspx
    /// </summary>
    public class PredicateBuilder<T>
    {
        private Expression expression;
        private ParameterExpression parameter;
        private Renamer renamer = new Renamer();

        /// <summary>
        /// Construct with initial expression
        /// </summary>
        /// <param name="expr"></param>
        public PredicateBuilder(Expression<Func<T, bool>> expr)
        {
            expression = expr.Body;
            parameter = expr.Parameters[0];
        }

        /// <summary>
        /// Extends expression with boolean or expression
        /// </summary>
        public void OrElse(Expression<Func<T, bool>> expr)
        {
            var renamedExpression = renamer.Rename(expr.Body, expr.Parameters[0], parameter);
            expression = Expression.OrElse(expression, renamedExpression);
        }

        /// <summary>
        /// Extends expression with boolean and expression
        /// </summary>
        public void AndAlso(Expression<Func<T, bool>> expr)
        {
            var renamedExpression = renamer.Rename(expr.Body, expr.Parameters[0], parameter);
            expression = Expression.AndAlso(expression, renamedExpression);
        }

        /// <summary>
        /// Returns the predicate
        /// </summary>
        public Expression<Func<T, bool>> GetPredicate()
        {
            return Expression.Lambda<Func<T, bool>>(expression, parameter);
        }

        private class Renamer : ExpressionVisitor
        {
            ParameterExpression FromExpression;
            ParameterExpression ToExpression;

            public Expression Rename(Expression expression, ParameterExpression fromExpression, ParameterExpression toExpression)
            {
                FromExpression = fromExpression;
                ToExpression = toExpression;
                return Visit(expression);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == FromExpression)
                {
                    return ToExpression;
                }
                else
                {
                    return node;
                }
            }
        }
    }
}
