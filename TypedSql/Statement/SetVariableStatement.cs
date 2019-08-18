using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface ISetVariableStatement : IStatement
    {
        SqlPlaceholder Variable { get; }
        LambdaExpression ValueExpression { get; }
        void EvaluateInMemory(IQueryRunner runner);
    }

    public class SetVariableStatement<T> : ISetVariableStatement where T : IComparable, IConvertible
    {
        public SqlPlaceholder Variable { get; }
        public SqlPlaceholder<T> VariableT { get; }
        public LambdaExpression ValueExpression { get; }
        private Func<SelectorContext<T>, T> ValueFunction { get; }

        public SetVariableStatement(SqlPlaceholder<T> variable, Expression<Func<SelectorContext<T>, T>> valueExpr)
        {
            Variable = variable;
            VariableT = variable;
            ValueExpression = valueExpr;
            ValueFunction = valueExpr.Compile();
        }

        public void EvaluateInMemory(IQueryRunner runner)
        {
            if (VariableT.PlaceholderType == SqlPlaceholderType.SessionVariableName)
            {
                var context = new SelectorContext<T>(runner, null);
                VariableT.Value = ValueFunction(context);
            }
            else
            {
                throw new InvalidOperationException("Uhndlet vrbitype");
            }
        }
    }
}
