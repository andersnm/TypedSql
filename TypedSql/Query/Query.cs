﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql {

    public class SelectorContext
    {
        internal readonly IQueryRunner Runner;

        public SelectorContext(IQueryRunner runner)
        {
            Runner = runner;
        }
    }

    public class SelectorContext<T>
    {
        internal readonly IQueryRunner Runner;
        internal readonly List<T> Items;

        public SelectorContext(IQueryRunner runner, List<T> items)
        {
            Runner = runner;
            Items = items;
        }
    }

    public abstract class Query {
        public Query(Query parent) {
            Parent = parent;
        }

        public Query Parent { get; }

        internal FromQuery<TFrom> GetFromQuery<TFrom>() where TFrom: new()
        {
            if (this is FromQuery<TFrom> result)
            {
                return result;
            }

            if (Parent == null)
            {
                return null;
            }

            return Parent.GetFromQuery<TFrom>();
        }
    }

    public abstract class Query<TFrom, T> : Query {
        public Query(Query parent) : base(parent) {}

        internal Dictionary<T, TFrom> FromRowMapping = new Dictionary<T, TFrom>();

        internal virtual IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            throw new NotSupportedException("Select must be overridden");
        }
    }

    public class FlatQuery<TFrom, T> : Query<TFrom, T>
    {
        public FlatQuery(Query parent) : base(parent) { }

        /// <summary>
        /// ... WHERE {where expression}
        /// </summary>
        public FlatQuery<TFrom, T> Where(Expression<Func<T, bool>> whereExpr)
        {
            return new WhereQuery<TFrom, T>(this, whereExpr);
        }

        /// <summary>
        /// ... INNER JOIN {table} ON {join expression} => {projection expression}
        /// </summary>
        public FlatQuery<TFrom, TKey> Join<TJoinFrom, TJoin, TKey>(Query<TJoinFrom, TJoin> table, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool>> joinExpr, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey>> keyExpr)
        {
            return new JoinQuery<TFrom, T, TJoinFrom, TJoin, TKey>(this, table, joinExpr, keyExpr, JoinType.InnerJoin);
        }

        /// <summary>
        /// ... LEFT JOIN {table} ON {join expression} => {projection expression}
        /// </summary>
        public FlatQuery<TFrom, TKey> LeftJoin<TJoinFrom, TJoin, TKey>(Query<TJoinFrom, TJoin> table, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool>> joinExpr, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey>> keyExpr)
        {
            return new JoinQuery<TFrom, T, TJoinFrom, TJoin, TKey>(this, table, joinExpr, keyExpr, JoinType.LeftJoin);
        }

        /// <summary>
        /// ... GROUP BY {group expression} => {projection expression}
        /// </summary>
        public AggregateQuery<TFrom, TProject> GroupBy<TGroup, TProject>(Expression<Func<T, TGroup>> groupExpr, Expression<Func<SelectorContext<T>, T, TProject>> projectExpr)
        {
            return new GroupByQuery<TFrom, T, TGroup, TProject>(this, groupExpr, projectExpr);
        }

        /// <summary>
        /// ... ORDER BY {selector}
        /// </summary>
        public FlatQuery<TFrom, T> OrderBy<FT>(bool ascending, Expression<Func<T, FT>> selector)
        {
            return new OrderByQuery<TFrom, T, FT>(this, ascending, selector);
        }

        /// <summary>
        /// ... OFFSET {offset}
        /// </summary>
        public FlatQuery<TFrom, T> Offset(int offset)
        {
            return new OffsetQuery<TFrom, T>(this, offset);
        }

        /// <summary>
        /// ... LIMIT {offset}
        /// </summary>
        public FlatQuery<TFrom, T> Limit(int limit)
        {
            return new LimitQuery<TFrom, T>(this, limit);
        }

        /// <summary>
        /// SELECT {expression} FROM ...
        /// </summary>
        public FlatQuery<TFrom, TKey> Select<TKey>(Expression<Func<SelectorContext<T>, T, TKey>> selectExpr)
        {
            return new SelectQuery<TFrom, T, TKey>(this, selectExpr);
        }

        /// <summary>
        /// Project specific fields in parent query
        /// </summary>
        public FlatQuery<TFrom, TKey> Project<TKey>(Expression<Func<SelectorContext<T>, T, TKey>> selectExpr)
        {
            return new ProjectQuery<TFrom, T, TKey>(this, selectExpr);
        }

        protected bool HasAggregates(Expression selectExpression)
        {
            var visitor = new AggregateVisitor();
            visitor.Visit(selectExpression);
            return visitor.CalledAggregateFunction;
        }

    }

    public class AggregateQuery<TFrom, T> : Query<TFrom, T>
    {
        public AggregateQuery(Query parent) : base(parent) { }

        public AggregateQuery<TFrom, T> Having(Expression<Func<T, bool>> whereExpr)
        {
            return new HavingQuery<TFrom, T>(this, whereExpr);
        }
    }

    class AggregateVisitor : ExpressionVisitor
    {
        public bool CalledAggregateFunction { get; set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Function))
            {
                if (node.Method.Name == nameof(Function.Count) || node.Method.Name == nameof(Function.Sum))
                {
                    CalledAggregateFunction = true;
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
