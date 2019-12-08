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
        public SelectStatement(Query<TFrom, T> parent)
        {
            SelectQuery = parent;
        }

        internal Query<TFrom, T> SelectQuery { get; }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            // Scan for limit/offset and handle here
            int? limit = null;
            int? offset = null;

            for (Query parent = SelectQuery; parent != null; parent = parent.Parent)
            {
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
                FromSource = SelectQuery.Parse(parser),
            };
        }
    }

    public class SelectStatement<TResult> : ISelectStatement
    {
        public SelectStatement(Expression<Func<SelectorContext, TResult>> selectExpression)
        {
            SelectQuery = new ProjectConstantQuery<TResult>(selectExpression);
        }

        public Query<TResult, TResult> SelectQuery { get; }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            return SelectQuery.InMemorySelect(runner).Cast<object>().ToList();
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlSelect()
            {
                FromSource = SelectQuery.Parse(parser)
            };
        }
    }
}
