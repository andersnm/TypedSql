using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class HavingQuery<TFrom, T> : AggregateQuery<TFrom, T>
    {
        public HavingQuery(Query<TFrom, T> parent, Expression<Func<T, bool>> expr)
            : base(parent)
        {
            ParentT = parent;
            HavingExpression = expr;
            HavingFunction = expr.Compile();
        }

        private LambdaExpression HavingExpression { get; }
        private Func<T, bool> HavingFunction { get; }
        private Query<TFrom, T> ParentT { get; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            return items.Where(x => HavingFunction(x));
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[HavingExpression.Parameters[0].Name] = parentResult;

            result.Havings.Add(parser.ParseExpression(HavingExpression.Body, parameters));
            return result;
        }
    }
}
