using System;
using System.Linq.Expressions;

namespace TypedSql
{
    public abstract class FlatQuery<TFrom, T> : Query<TFrom, T>
    {
        public FlatQuery(Query parent)
            : base(parent)
        {
        }

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
        public FlatQuery<TFrom, T> OrderBy(Expression<Action<OrderByBuilder<T>>> builderExpr)
        {
            return new OrderByQuery<TFrom, T>(this, builderExpr);
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
}
