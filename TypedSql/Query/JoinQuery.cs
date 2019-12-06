using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public enum JoinType
    {
        InnerJoin,
        LeftJoin,
        CrossJoin
    }

    public interface IJoinQuery
    {
        Query Parent { get; }
        Query JoinTable { get; }
        LambdaExpression JoinExpression { get; }
        LambdaExpression ResultExpression { get; }
        JoinType JoinType { get; }
    }

    public class JoinQuery<TFrom, T, TJoinFrom, TJoin, TKey> : FlatQuery<TFrom, TKey>, IJoinQuery
    {
        public Query<TFrom, T> ParentT { get; }
        public Query JoinTable { get; }
        public Query<TJoinFrom, TJoin> JoinTableTJoin { get; }
        public LambdaExpression JoinExpression { get; }
        public LambdaExpression ResultExpression { get; }
        public JoinType JoinType { get; }
        private Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool> JoinFunction { get; }
        private Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey> ResultFunction { get; }

        public JoinQuery(Query<TFrom, T> parent, Query<TJoinFrom, TJoin> joinTable, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool>> joinExpr, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey>> resultExpr, JoinType type) 
            : base(parent)
        {
            ParentT = parent;
            JoinTable = joinTable;
            JoinTableTJoin = joinTable;
            JoinExpression = joinExpr;
            ResultExpression = resultExpr;
            JoinType = type;
            JoinFunction = joinExpr.Compile();
            ResultFunction = resultExpr.Compile();
        }

        internal override IEnumerable<TKey> InMemorySelect(IQueryRunner runner)
        {
            var lhs = ParentT.InMemorySelect(runner).ToList();
            var rhs = JoinTableTJoin.InMemorySelect(runner).ToList();

            var ctx = new SelectorContext<T>(runner, lhs);
            var joinCtx = new SelectorContext<TJoin>(runner, rhs);

            foreach (var item in lhs)
            {
                var joined = false;
                var fromRow = ParentT.FromRowMapping[item];

                foreach (var joinItem in rhs)
                {
                    var joinResult = JoinFunction(ctx, item, joinCtx, joinItem);
                    if (joinResult)
                    {
                        joined = true;
                        var result = ResultFunction(ctx, item, joinCtx, joinItem);
                        FromRowMapping[result] = fromRow;
                        yield return result;
                    }
                }

                if (!joined && JoinType == JoinType.LeftJoin)
                {
                    var result = ResultFunction(ctx, item, joinCtx, default(TJoin));
                    FromRowMapping[result] = fromRow;
                    yield return result;
                }
            }
        }
    }
}
