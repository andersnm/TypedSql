using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public abstract class OrderByItem
    {
        public OrderByItem(LambdaExpression selector, bool ascending)
        {
            Selector = selector;
            Ascending = ascending;
        }

        public LambdaExpression Selector { get; }
        public bool Ascending { get; }
    }

    public abstract class OrderByItem<T> : OrderByItem
    {
        public OrderByItem(LambdaExpression selector, bool ascending)
            : base(selector, ascending)
        {
        }

        internal abstract IOrderedEnumerable<T> OrderBy(IEnumerable<T> parent);
        internal abstract IOrderedEnumerable<T> ThenBy(IOrderedEnumerable<T> parent);
    }

    public class OrderByItem<T, TValueType> : OrderByItem<T>
    {
        public OrderByItem(Expression<Func<T, TValueType>> selector, bool ascending)
            : base(selector, ascending)
        {
            SelectorFunction = selector.Compile();
        }

        public Func<T, TValueType> SelectorFunction { get; }

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
                return parent.ThenBy(f => SelectorFunction(f));
            }
            else
            {
                return parent.ThenByDescending(f => SelectorFunction(f));
            }
        }
    }

    public class OrderByBuilder<T>
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
}
