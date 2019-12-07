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
        public Func<T, TValueType> SelectorFunction { get;  }

        public OrderByItem(Expression<Func<T, TValueType>> selector, bool ascending)
            : base(selector, ascending)
        {
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

    public class OrderByQuery<TFrom, T> : FlatQuery<TFrom, T>
    {
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

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);
            result.OrderBys = parser.ParseOrderByBuilder<T>(parentResult, OrderByBuilderExpression, new Dictionary<string, SqlSubQueryResult>());
            return result;
        }
    }

    public interface IOffsetQuery
    {
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

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);
            result.Offset = OffsetIndex;
            return result;
        }
    }

    public interface ILimitQuery
    {
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

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);
            result.Limit = LimitIndex;
            return result;
        }
    }
}
