﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class OrderByQuery<TFrom, T> : FlatQuery<TFrom, T>
    {
        public OrderByQuery(Query<TFrom, T> parent, Expression<Action<OrderByBuilder<T>>> builderExpr)
            : base(parent)
        {
            ParentT = parent;
            OrderByBuilderExpression = builderExpr;
            OrderByBuilderFunction = builderExpr.Compile();
        }

        private Query<TFrom, T> ParentT { get; }
        private LambdaExpression OrderByBuilderExpression { get; }
        private Action<OrderByBuilder<T>> OrderByBuilderFunction { get; }

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

        internal override SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, parameters, out parentResult);
            result.OrderBys = parser.ParseOrderByBuilder<T>(parentResult, OrderByBuilderExpression, parameters);
            return result;
        }
    }
}
