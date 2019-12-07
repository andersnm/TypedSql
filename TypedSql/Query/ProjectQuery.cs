﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class ProjectQuery<TFrom, T, TResult> : FlatQuery<TFrom, TResult>
    {
        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression SelectExpression { get; }
        private Func<SelectorContext<T>, T, TResult> SelectFunction { get; set; }

        public ProjectQuery(Query<TFrom, T> parent, Expression<Func<SelectorContext<T>, T, TResult>> selectExpression)
            : base(parent)
        {
            ParentT = parent;
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

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

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out var tempParentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>();

            parameters[SelectExpression.Parameters[0].Name] = tempParentResult; // ctx
            parameters[SelectExpression.Parameters[1].Name] = tempParentResult; // item

            parentResult = new SqlSubQueryResult()
            {
                Members = parser.ParseSelectExpression(SelectExpression, parameters)
            };

            return result;
        }

        private TResult InvokeSelectFunction(SelectorContext<T> context, T item)
        {
            var result = SelectFunction(context, item);

            if (ParentT is FlatQuery<TFrom, T>)
            {
                var fromRow = ParentT.FromRowMapping[item];
                FromRowMapping[result] = fromRow;
            }

            return result;
        }
    }

    public class ProjectConstantQuery<T> : FlatQuery<T, T>
    {
        public LambdaExpression SelectExpression { get; }
        private Func<SelectorContext, T> SelectFunction { get; set; }

        public ProjectConstantQuery(Expression<Func<SelectorContext, T>> selectExpression)
            : base(null)
        {
            SelectExpression = selectExpression;
            SelectFunction = selectExpression.Compile();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var context = new SelectorContext(runner);

            return new List<T>()
            {
                SelectFunction(context)
            };
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            // No parent, create new SqlQuery
            var result = new SqlQuery();
            var tempParentResult = new SqlSubQueryResult()
            {
                Members = new List<SqlMember>(),
            };

            var parameters = new Dictionary<string, SqlSubQueryResult>();

            parameters[SelectExpression.Parameters[0].Name] = tempParentResult; // ctx

            parentResult = new SqlSubQueryResult()
            {
                Members = parser.ParseSelectExpression(SelectExpression, parameters)
            };

            return result;
        }
    }
}
