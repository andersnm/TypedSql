using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IHavingQuery
    {
        LambdaExpression HavingExpression { get; }
    }

    public class HavingQuery<TFrom, T> : AggregateQuery<TFrom, T>, IHavingQuery
    {
        public LambdaExpression HavingExpression { get; }
        private Func<T, bool> HavingFunction { get; }
        private Query<TFrom, T> ParentT { get; }

        public HavingQuery(Query<TFrom, T> parent, Expression<Func<T, bool>> expr)
            : base(parent)
        {
            ParentT = parent;
            HavingExpression = expr;
            HavingFunction = expr.Compile();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            return items.Where(x => HavingFunction(x));
        }
    }
}
