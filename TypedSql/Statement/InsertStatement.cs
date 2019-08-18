using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface IInsertStatement : IStatement
    {
        IFromQuery FromQuery { get; }
        LambdaExpression InsertExpression { get; }

        int EvaluateInMemory(InMemoryQueryRunner runner, out int identity);
    }

    public class InsertStatement<T> : IInsertStatement where T: new()
    {
        public IFromQuery FromQuery { get; }
        public LambdaExpression InsertExpression { get; }

        internal FromQuery<T> FromQueryT { get; }
        internal Action<InsertBuilder<T>> InsertFunction { get; }

        public InsertStatement(FromQuery<T> parent, Expression<Action<InsertBuilder<T>>> insertExpr)
        {
            FromQuery = parent;
            FromQueryT = parent;
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        public int EvaluateInMemory(InMemoryQueryRunner runner, out int identity)
        {
            var builder = new InsertBuilder<T>();
            InsertFunction(builder);
            FromQueryT.InsertImpl(builder, out identity);
            return 1;
        }
    }
}
