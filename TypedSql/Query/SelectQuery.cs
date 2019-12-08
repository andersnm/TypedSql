using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class SelectQuery<TFrom, T, TResult> : FlatQuery<TFrom, TResult>
    {
        public SelectQuery(Query<TFrom, T> parent, Expression<Func<SelectorContext<T>, T, TResult>> selectExpression)
            : base(parent)
        {
            ParentT = parent;
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

        private Query<TFrom, T> ParentT { get; }
        private LambdaExpression SelectExpression { get; }
        private Func<SelectorContext<T>, T, TResult> SelectFunction { get; set; }

        internal override IEnumerable<TResult> InMemorySelect(IQueryRunner runner)
        {
            var parentResult = ParentT.InMemorySelect(runner);
            var context = new SelectorContext<T>(runner, parentResult.ToList());

            // Implicit grouping, f.ex SELECT COUNT(*) FROM tbl
            if (ParentT is FlatQuery<TFrom, T> && HasAggregates(SelectExpression))
            {
                return parentResult.Select(x => InvokeSelectFunction(context, x)).Take(1);
            }

            return parentResult.Select(x => InvokeSelectFunction(context, x));
        }

        internal override SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult)
        {
            var joinAlias = parser.AliasProvider.CreateAlias();
            var result = ParentT.Parse(parser, parameters, out var tempParentResult);
            var selectParameters = new Dictionary<string, SqlSubQueryResult>(parameters);

            selectParameters[SelectExpression.Parameters[0].Name] = tempParentResult; // ctx
            selectParameters[SelectExpression.Parameters[1].Name] = tempParentResult; // item

            result.SelectResult = new SqlSubQueryResult()
            {
                Members = parser.ParseSelectExpression(SelectExpression, selectParameters)
            };

            parentResult = new SqlSubQueryResult()
            {
                Members = result.SelectResult.Members.Select(m => new SqlJoinFieldMember()
                {
                    JoinAlias = joinAlias,
                    SourceField = m,
                    MemberName = m.MemberName,
                    MemberInfo = m.MemberInfo,
                    SqlName = m.SqlName,
                    FieldType = m.FieldType,
                }).ToList<SqlMember>(),
            };

            return new SqlQuery()
            {
                From = new SqlFromSubQuery()
                {
                    FromQuery = result,
                },
                FromAlias = joinAlias,
            };
        }

        private TResult InvokeSelectFunction(SelectorContext<T> context, T item)
        {
            var result = SelectFunction(context, item);
            var fromRow = ParentT.FromRowMapping[item];
            FromRowMapping[result] = fromRow;
            return result;
        }
    }
}
