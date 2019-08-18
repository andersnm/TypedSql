using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql {
    public interface IOrderByQuery {
        Query Parent { get; }
        LambdaExpression SelectorExpression { get; }
        bool Ascending { get; }
    }

    public class OrderByQuery<TFrom, T, FT> : FlatQuery<TFrom, T>, IOrderByQuery {

        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression SelectorExpression { get; }
        public bool Ascending { get; }
        public Func<T, FT> SelectorFunction { get; }

        public OrderByQuery(Query<TFrom, T> parent, bool ascending, Expression<Func<T, FT>> selector)
            : base(parent)
        {
            ParentT = parent;
            Ascending = ascending;
            SelectorExpression = selector;
            SelectorFunction = selector.Compile();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            if (Ascending)
            {
                return items.OrderBy(x => SelectorFunction(x));
            }
            else
            {
                return items.OrderByDescending(x => SelectorFunction(x));
            }
        }

    }

    public interface IOffsetQuery
    {
        Query Parent { get; }
        int OffsetIndex { get; }
    }

    public class OffsetQuery<TFrom, T> : FlatQuery<TFrom, T>, IOffsetQuery
    {
        public int OffsetIndex { get; }

        public OffsetQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            OffsetIndex = offset;
        }
    }

    public interface ILimitQuery
    {
        Query Parent { get; }
        int LimitIndex { get; }
    }

    public class LimitQuery<TFrom, T> : FlatQuery<TFrom, T>, ILimitQuery
    {
        public int LimitIndex { get; }

        public LimitQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            LimitIndex = offset;
        }
    }
}
