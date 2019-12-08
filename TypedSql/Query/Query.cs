using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedSql
{
    public abstract class Query
    {
        public Query(Query parent)
        {
            Parent = parent;
        }

        public Query Parent { get; }

        internal FromQuery<TFrom> GetFromQuery<TFrom>()
            where TFrom : new()
        {
            if (this is FromQuery<TFrom> result)
            {
                return result;
            }

            if (Parent == null)
            {
                return null;
            }

            return Parent.GetFromQuery<TFrom>();
        }

        internal SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters)
        {
            var result = Parse(parser, parameters, out var selectResult);
            result.SelectResult = selectResult;
            return result;
        }

        internal abstract SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult);
    }

    public abstract class Query<TFrom, T> : Query
    {
        public Query(Query parent)
            : base(parent)
        {
        }

        internal Dictionary<T, TFrom> FromRowMapping { get; set; } = new Dictionary<T, TFrom>();

        public T AsExpression(SelectorContext ctx)
        {
            return InMemorySelect(ctx.Runner).FirstOrDefault();
        }

        internal abstract IEnumerable<T> InMemorySelect(IQueryRunner runner);
    }
}
