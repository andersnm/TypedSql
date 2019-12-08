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

    public class JoinQuery<TFrom, T, TJoinFrom, TJoin, TKey> : FlatQuery<TFrom, TKey>
    {
        public JoinQuery(Query<TFrom, T> parent, Query<TJoinFrom, TJoin> joinTable, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool>> joinExpr, Expression<Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey>> resultExpr, JoinType type)
            : base(parent)
        {
            ParentT = parent;
            JoinTable = joinTable;
            JoinExpression = joinExpr;
            ResultExpression = resultExpr;
            JoinType = type;
            JoinFunction = joinExpr.Compile();
            ResultFunction = resultExpr.Compile();
        }

        private Query<TFrom, T> ParentT { get; }
        private Query<TJoinFrom, TJoin> JoinTable { get; }
        private LambdaExpression JoinExpression { get; }
        private LambdaExpression ResultExpression { get; }
        private JoinType JoinType { get; }
        private Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, bool> JoinFunction { get; }
        private Func<SelectorContext<T>, T, SelectorContext<TJoin>, TJoin, TKey> ResultFunction { get; }

        internal override IEnumerable<TKey> InMemorySelect(IQueryRunner runner)
        {
            var lhs = ParentT.InMemorySelect(runner).ToList();
            var rhs = JoinTable.InMemorySelect(runner).ToList();

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

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out var tempParentResult);
            var joinFromSubQuery = JoinTable.Parse(parser, out var joinFromResult);
            joinFromSubQuery.SelectResult = joinFromResult;

            if (JoinTable is IFromQuery)
            {
                ParseJoinTableQuery(parser, tempParentResult, joinFromSubQuery, result, out parentResult);
                return result;
            }
            else
            {
                ParseJoinSubQuery(parser, tempParentResult, joinFromSubQuery, result, out parentResult);
                return result;
            }
        }

        private void ParseJoinSubQuery(SqlQueryParser parser, SqlSubQueryResult parentResult, SqlQuery joinFromSubQuery, SqlQuery result, out SqlSubQueryResult joinResult)
        {
            var joinAlias = parser.AliasProvider.CreateAlias();

            // Translate fields from inside the subquery to fields rooted in the subquery usable from the outside
            var tempFromSubQuery = new SqlSubQueryResult()
            {
                Members = joinFromSubQuery.SelectResult.Members.Select(m => new SqlJoinFieldMember()
                {
                    JoinAlias = joinAlias,
                    SourceField = m,
                    MemberName = m.MemberName,
                    MemberInfo = m.MemberInfo,
                    SqlName = m.MemberName,
                    FieldType = m.FieldType,
                }).ToList<SqlMember>(),
            };

            // Parameters in both selector & join expressions: actx, a, bctx, b
            var outerParameters = new Dictionary<string, SqlSubQueryResult>();
            outerParameters[JoinExpression.Parameters[0].Name] = parentResult;
            outerParameters[JoinExpression.Parameters[1].Name] = parentResult;
            outerParameters[JoinExpression.Parameters[2].Name] = tempFromSubQuery;
            outerParameters[JoinExpression.Parameters[3].Name] = tempFromSubQuery;

            var joinResultMembers = parser.ParseSelectExpression(ResultExpression, outerParameters);

            var joinInfo = new SqlJoinSubQuery()
            {
                JoinAlias = joinAlias,
                JoinResult = new SqlSubQueryResult()
                {
                    Members = joinResultMembers,
                },
                JoinFrom = joinFromSubQuery,
                JoinExpression = parser.ParseExpression(JoinExpression.Body, outerParameters),
                JoinType = JoinType,
            };

            result.Joins.Add(joinInfo);
            joinResult = joinInfo.JoinResult;
        }

        private void ParseJoinTableQuery(SqlQueryParser parser, SqlSubQueryResult parentResult, SqlQuery joinFromSubQuery, SqlQuery result, out SqlSubQueryResult joinResult)
        {
            // Parameters in both selector & join expressions: actx, a, bctx, b
            var outerParameters = new Dictionary<string, SqlSubQueryResult>();
            outerParameters[JoinExpression.Parameters[0].Name] = parentResult;
            outerParameters[JoinExpression.Parameters[1].Name] = parentResult;
            outerParameters[JoinExpression.Parameters[2].Name] = joinFromSubQuery.SelectResult;
            outerParameters[JoinExpression.Parameters[3].Name] = joinFromSubQuery.SelectResult;

            var joinResultMembers = parser.ParseSelectExpression(ResultExpression, outerParameters);

            var joinInfo = new SqlJoinTable()
            {
                TableAlias = joinFromSubQuery.FromAlias,
                FromSource = joinFromSubQuery.From,
                JoinExpression = parser.ParseExpression(JoinExpression.Body, outerParameters),
                JoinResult = new SqlSubQueryResult()
                {
                    Members = joinResultMembers,
                },
                JoinType = JoinType,
            };

            result.Joins.Add(joinInfo);

            joinResult = joinInfo.JoinResult;
        }
    }
}
