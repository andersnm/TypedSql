using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface ISetVariableStatement : IStatement
    {
        void EvaluateInMemory(IQueryRunner runner);
    }

    public class SetVariableStatement<T> : ISetVariableStatement
    {
        public SqlPlaceholder<T> Variable { get; }
        public Expression<Func<SelectorContext<T>, T>> ValueExpression { get; }
        private Func<SelectorContext<T>, T> ValueFunction { get; }

        public SetVariableStatement(SqlPlaceholder<T> variable, Expression<Func<SelectorContext<T>, T>> valueExpr)
        {
            Variable = variable;
            ValueExpression = valueExpr;
            ValueFunction = valueExpr.Compile();
        }

        public void EvaluateInMemory(IQueryRunner runner)
        {
            if (Variable.PlaceholderType == SqlPlaceholderType.SessionVariableName)
            {
                var context = new SelectorContext<T>(runner, null);
                Variable.Value = ValueFunction(context);
            }
            else
            {
                throw new InvalidOperationException("Uhndlet vrbitype");
            }
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlSet()
            {
                Expression = parser.ParseExpression(ValueExpression),
                Variable = Variable,
            };
        }

    }
}
