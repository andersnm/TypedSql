using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class WhereQuery<TFrom, T> : FlatQuery<TFrom, T>
    {
        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression WhereExpression { get; }
        private Func<T, bool> WhereFunction { get; }

        public WhereQuery(Query<TFrom, T> parent, Expression<Func<T, bool>> expr) 
            : base(parent)
        {
            ParentT = parent;
            WhereExpression = expr;
            WhereFunction = expr.Compile();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            return items.Where(x => WhereFunction(x));
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[WhereExpression.Parameters[0].Name] = parentResult;

            result.Wheres.Add(parser.ParseExpression(WhereExpression.Body, parameters));
            return result;
        }
    }
}
