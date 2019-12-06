using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IProjectQuery
    {
        Query Parent { get; }
        // two args: ctx, itemT
        LambdaExpression SelectExpression { get; }
    }

    public class ProjectQuery<TFrom, T, TResult> : FlatQuery<TFrom, TResult>, IProjectQuery
    {
        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression SelectExpression { get; }
        private Func<SelectorContext<T>, T, TResult> SelectFunction { get; set; }

        public ProjectQuery(Query<TFrom, T> parent, Expression<Func<SelectorContext<T>, T, TResult>> selectExpression)
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

            if (Parent is FlatQuery<TFrom, T>)
            {
                var fromRow = ParentT.FromRowMapping[item];
                FromRowMapping[result] = fromRow;
            }

            return result;
        }
    }

    public interface IProjectConstantQuery
    {
        // One arg: ctx
        LambdaExpression SelectExpression { get; }
    }

    public class ProjectConstantQuery<T> : FlatQuery<T, T>, IProjectConstantQuery
    {
        public LambdaExpression SelectExpression { get; }
        private Func<SelectorContext, T> SelectFunction { get; set; }

        public ProjectConstantQuery(Expression<Func<SelectorContext, T>> selectExpression)
            : base(null)
        {
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var context = new SelectorContext(runner);

            return new List<T>()
            {
                SelectFunction(context)
            };
        }
    }
}
