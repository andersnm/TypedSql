using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface IInsertSelectStatement : IStatement
    {
        IFromQuery FromQuery { get; }
        LambdaExpression InsertExpression { get; }
        Query SelectQuery { get; }
        int EvaluateInMemory(InMemoryQueryRunner runner, out int identity);
    }

    public class InsertSelectStatement<T, STFrom, ST> : IInsertSelectStatement where T: new()
    {
        public IFromQuery FromQuery { get; }
        public LambdaExpression InsertExpression { get; }
        public Query SelectQuery { get; }

        private FromQuery<T> FromQueryT { get; }
        private Query<STFrom, ST> SelectQueryST { get; }
        private Action<ST, InsertBuilder<T>> InsertFunction { get; }

        public InsertSelectStatement(FromQuery<T> parent, Query<STFrom, ST> insertSelectQuery, Expression<Action<ST, InsertBuilder<T>>> insertExpr)
        {
            FromQuery = parent;
            FromQueryT = parent;
            SelectQuery = insertSelectQuery;
            SelectQueryST = insertSelectQuery;
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        public int EvaluateInMemory(InMemoryQueryRunner runner, out int identity)
        {
            var lastNonQueryResult = 0;
            identity = 0;

            var items = SelectQueryST.InMemorySelect(runner);
            foreach (var item in items)
            {
                var builder = new InsertBuilder<T>();
                InsertFunction(item, builder);

                FromQueryT.InsertImpl(builder, out identity);
                lastNonQueryResult++;
            }

            return lastNonQueryResult;
        }
    }
}
