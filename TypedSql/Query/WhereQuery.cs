using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql {
    public interface IWhereQuery {
        Query Parent { get; }
        LambdaExpression WhereExpression { get; }
    }

    public class WhereQuery<TFrom, T> : FlatQuery<TFrom, T>, IWhereQuery {

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
    }
}
