using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface IInsertStatement : IStatement
    {
        int EvaluateInMemory(InMemoryQueryRunner runner, out int identity);
    }

    public class InsertStatement<T> : IInsertStatement
        where T : new()
    {
        internal FromQuery<T> FromQuery { get; }
        public Expression<Action<InsertBuilder<T>>> InsertExpression { get; }
        internal Action<InsertBuilder<T>> InsertFunction { get; }

        public InsertStatement(FromQuery<T> parent, Expression<Action<InsertBuilder<T>>> insertExpr)
        {
            FromQuery = parent;
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        public int EvaluateInMemory(InMemoryQueryRunner runner, out int identity)
        {
            var builder = new InsertBuilder<T>();
            InsertFunction(builder);
            FromQuery.InsertImpl(builder, out identity);
            return 1;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            var inserts = parser.ParseInsertBuilder<T>(FromQuery, InsertExpression, parameters);

            var primaryKey = FromQuery.Columns.Where(c => c.PrimaryKeyAutoIncrement).FirstOrDefault();

            return new SqlInsert()
            {
                Inserts = inserts,
                TableName = FromQuery.TableName,
                AutoIncrementPrimaryKeyName = primaryKey?.SqlName,
            };
        }
    }
}
