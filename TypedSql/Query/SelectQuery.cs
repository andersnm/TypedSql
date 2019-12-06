using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TypedSql
{
    public interface ISelectQuery
    {
        Query Parent { get; }
        LambdaExpression SelectExpression { get; }
    }

    public class SelectQuery<TFrom, T, TResult> : FlatQuery<TFrom, TResult>, ISelectQuery
    {
        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression SelectExpression { get; }
        private Func<SelectorContext<T>, T, TResult> SelectFunction { get; set; }

        public SelectQuery(Query<TFrom, T> parent, Expression<Func<SelectorContext<T>, T, TResult>> selectExpression)
            : base(parent)
        {
            ParentT = parent;
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

        internal override IEnumerable<TResult> InMemorySelect(IQueryRunner runner)
        {
            var parentResult = ParentT.InMemorySelect(runner);
            var context = new SelectorContext<T>(runner, parentResult.ToList());

            // Implicit grouping, f.ex SELECT COUNT(*) FROM tbl
            if (Parent is FlatQuery<TFrom, T> && HasAggregates(SelectExpression))
            {
                return parentResult.Select(x => InvokeSelectFunction(context, x)).Take(1);
            }

            return parentResult.Select(x => InvokeSelectFunction(context, x));
        }

        TResult InvokeSelectFunction(SelectorContext<T> context, T item)
        {
            var result = SelectFunction(context, item);
            var fromRow = ParentT.FromRowMapping[item];
            FromRowMapping[result] = fromRow;
            return result;
        }
    }
}
