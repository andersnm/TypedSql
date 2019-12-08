using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IUpdateStatement : IStatement
    {
        int EvaluateInMemory(IQueryRunner runner);
    }

    public class UpdateStatement<T, TJoin> : IUpdateStatement
        where T : new()
    {
        /// <summary>
        /// UPDATE ... SET with row parameter
        /// </summary>
        public UpdateStatement(FlatQuery<T, TJoin> query, Expression<Action<TJoin, InsertBuilder<T>>> insertExpr)
        {
            Parent = query;
            FromQuery = query.GetFromQuery<T>();
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        private Expression<Action<TJoin, InsertBuilder<T>>> InsertExpression { get; }
        private Query<T, TJoin> Parent { get; }
        private FromQuery<T> FromQuery { get; }
        private Action<TJoin, InsertBuilder<T>> InsertFunction { get; }

        public int EvaluateInMemory(IQueryRunner runner)
        {
            var lastNonQueryResult = 0;

            var items = Parent.InMemorySelect(runner);

            foreach (var item in items)
            {
                var fromRow = Parent.FromRowMapping[item];
                var builder = new InsertBuilder<T>();
                InsertFunction(item, builder);

                FromQuery.UpdateObject(fromRow, builder.BuilderType, builder);
                lastNonQueryResult++;
            }

            return lastNonQueryResult;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var queryResult = Parent.Parse(parser);
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[InsertExpression.Parameters[0].Name] = queryResult.SelectResult; // item
            // parameters[stmt.InsertExpression.Parameters[1].Name] = ; // builder

            var inserts = parser.ParseInsertBuilder<T>(FromQuery, InsertExpression, parameters);

            return new SqlUpdate()
            {
                FromSource = queryResult,
                Inserts = inserts,
            };
        }
    }
}
