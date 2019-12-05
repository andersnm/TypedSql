using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql {
    public interface OrderByItem
    {
        LambdaExpression Selector { get; }
        bool Ascending { get; }
    }

    public abstract class OrderByItem<T> : OrderByItem
    {
        public LambdaExpression Selector { get; set; }
        public bool Ascending { get; set; }

        internal abstract IOrderedEnumerable<T> OrderBy(IEnumerable<T> parent);
        internal abstract IOrderedEnumerable<T> ThenBy(IOrderedEnumerable<T> parent);
    }

    public class OrderByItem<T, TValueType> : OrderByItem<T>
    {
        public Expression<Func<T, TValueType>> SelectorT { get; set; }
        public Func<T, TValueType> SelectorFunction { get;  }

        public OrderByItem(Expression<Func<T, TValueType>> selector, bool ascending)
        {
            Selector = selector;
            SelectorT = selector;
            Ascending = ascending;
            SelectorFunction = selector.Compile();
        }

        internal override IOrderedEnumerable<T> OrderBy(IEnumerable<T> parent)
        {
            if (Ascending)
            {
                return parent.OrderBy(f => SelectorFunction(f));
            }
            else
            {
                return parent.OrderByDescending(f => SelectorFunction(f));
            }
        }

        internal override IOrderedEnumerable<T> ThenBy(IOrderedEnumerable<T> parent)
        {
            if (Ascending)
            {
                return parent.ThenBy(f => SelectorT.Compile()(f));
            }
            else
            {
                return parent.ThenByDescending(f => SelectorT.Compile()(f));
            }
        }
    }

    public interface IOrderByBuilder
    {
        List<OrderByItem> Selectors { get; }
    }

    public class OrderByBuilder<T> : IOrderByBuilder
    {
        public List<OrderByItem> Selectors { get; } = new List<OrderByItem>();

        public OrderByBuilder<T> Value<TValueType>(Expression<Func<T, TValueType>> selector, bool ascending)
        {
            Selectors.Add(new OrderByItem<T, TValueType>(selector, ascending));
            return this;
        }

        public OrderByBuilder<T> Values(OrderByBuilder<T> other)
        {
            Selectors.AddRange(other.Selectors);
            return this;
        }
    }

    public interface IOrderByQuery {
        Query Parent { get; }
        LambdaExpression OrderByBuilderExpression { get; }
    }

    public class OrderByQuery<TFrom, T> : FlatQuery<TFrom, T>, IOrderByQuery {

        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression OrderByBuilderExpression { get; }
        internal Action<OrderByBuilder<T>> OrderByBuilderFunction { get; }

        public OrderByQuery(Query<TFrom, T> parent, Expression<Action<OrderByBuilder<T>>> builderExpr)
            : base(parent)
        {
            ParentT = parent;
            OrderByBuilderExpression = builderExpr;
            OrderByBuilderFunction = builderExpr.Compile();

        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;

            var builder = new OrderByBuilder<T>();
            OrderByBuilderFunction(builder);

            IOrderedEnumerable<T> ordered = null;
            foreach (OrderByItem<T> selector in builder.Selectors)
            {
                if (ordered == null)
                {
                    ordered = selector.OrderBy(items);
                }
                else
                {
                    ordered = selector.ThenBy(ordered);
                }
            }

            if (ordered == null)
            {
                return items;
            }

            return ordered;
        }

    }

    public interface IOffsetQuery
    {
        Query Parent { get; }
        int OffsetIndex { get; }
    }

    public class OffsetQuery<TFrom, T> : FlatQuery<TFrom, T>, IOffsetQuery
    {
        public Query<TFrom, T> ParentT { get; }
        public int OffsetIndex { get; }

        public OffsetQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            ParentT = parent;
            OffsetIndex = offset;
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            // Not offsetting here, this is done in the select statement
            return items;
        }
    }

    public interface ILimitQuery
    {
        Query Parent { get; }
        int LimitIndex { get; }
    }

    public class LimitQuery<TFrom, T> : FlatQuery<TFrom, T>, ILimitQuery
    {
        public Query<TFrom, T> ParentT { get; }
        public int LimitIndex { get; }

        public LimitQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            ParentT = parent;
            LimitIndex = offset;
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            // Not limiting here, this is done in the select statement
            return items;
        }

    }
}
