using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface ISelectStatement : IStatement
    {
        List<object> EvaluateInMemory(InMemoryQueryRunner runner);
    }

    public class SelectStatement<TFrom, T> : ISelectStatement
    {
        internal Query<TFrom, T> SelectQuery { get; }

        public SelectStatement(Query<TFrom, T> parent)
        {
            SelectQuery = parent;
        }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            // Scan for limit/offset and handle here
            int? limit = null;
            int? offset = null;

            for (Query parent = SelectQuery; parent != null; parent = parent.Parent) {
                if (limit == null && parent is ILimitQuery limitQuery)
                {
                    limit = limitQuery.LimitIndex;
                }

                if (offset == null && parent is IOffsetQuery offsetQuery)
                {
                    offset = offsetQuery.OffsetIndex;
                }

                if (offset != null && limit != null)
                {
                    break;
                }
            }

            var result = SelectQuery.InMemorySelect(runner).Cast<object>();
            if (offset.HasValue)
            {
                result = result.Skip(offset.Value);
            }

            if (limit.HasValue)
            {
                result = result.Take(limit.Value);
            }

            return result.ToList();
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlSelect()
            {
                FromSource = parser.ParseQuery(SelectQuery),
            };
        }
    }

    public class SelectStatement<TResult> : ISelectStatement
    {
        public Query SelectQuery { get; }
        public Query<TResult, TResult> SelectQueryTResult { get; }

        public SelectStatement(Expression<Func<SelectorContext, TResult>> selectExpression)
        {
            SelectQueryTResult = new ProjectConstantQuery<TResult>(selectExpression);
            SelectQuery = SelectQueryTResult;
        }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            return SelectQueryTResult.InMemorySelect(runner).Cast<object>().ToList();
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlSelect()
            {
                FromSource = parser.ParseQuery(SelectQuery)
            };
        }
    }
}
