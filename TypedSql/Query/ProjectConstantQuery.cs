using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypedSql
{
    public class ProjectConstantQuery<T> : FlatQuery<T, T>
    {
        public ProjectConstantQuery(Expression<Func<SelectorContext, T>> selectExpression)
            : base(null)
        {
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

        private LambdaExpression SelectExpression { get; }
        private Func<SelectorContext, T> SelectFunction { get; set; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var context = new SelectorContext(runner);

            return new List<T>()
            {
                SelectFunction(context)
            };
        }

        internal override SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult)
        {
            // No parent, create new SqlQuery
            var result = new SqlQuery();
            var tempParentResult = new SqlSubQueryResult()
            {
                Members = new List<SqlMember>(),
            };

            var projectParameters = new Dictionary<string, SqlSubQueryResult>(parameters);

            projectParameters[SelectExpression.Parameters[0].Name] = tempParentResult; // ctx

            parentResult = new SqlSubQueryResult()
            {
                Members = parser.ParseSelectExpression(SelectExpression, projectParameters)
            };

            return result;
        }
    }
}
