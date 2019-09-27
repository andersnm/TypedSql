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
        int EvaluateInMemory(InMemoryQueryRunner runner, out int identity);
    }

    public class InsertSelectStatement<T, STFrom, ST> : IInsertSelectStatement where T: new()
    {
        private FromQuery<T> FromQuery { get; }
        private Query<STFrom, ST> SelectQuery { get; }
        public Expression<Action<ST, InsertBuilder<T>>> InsertExpression { get; }

        private Action<ST, InsertBuilder<T>> InsertFunction { get; }

        public InsertSelectStatement(FromQuery<T> parent, Query<STFrom, ST> insertSelectQuery, Expression<Action<ST, InsertBuilder<T>>> insertExpr)
        {
            FromQuery = parent;
            SelectQuery = insertSelectQuery;
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        public int EvaluateInMemory(InMemoryQueryRunner runner, out int identity)
        {
            var lastNonQueryResult = 0;
            identity = 0;

            var items = SelectQuery.InMemorySelect(runner);
            foreach (var item in items)
            {
                var builder = new InsertBuilder<T>();
                InsertFunction(item, builder);

                FromQuery.InsertImpl(builder, out identity);
                lastNonQueryResult++;
            }

            return lastNonQueryResult;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var parentQueryResult = parser.ParseQuery(SelectQuery);

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[InsertExpression.Parameters[0].Name] = parentQueryResult.SelectResult;

            var inserts = parser.ParseInsertBuilder(FromQuery, InsertExpression, parameters);

            return new SqlInsertSelect()
            {
                FromSource = parentQueryResult,
                Inserts = inserts,
                TableName = FromQuery.TableName,
            };
        }
    }
}
