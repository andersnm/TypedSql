﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public class WhereQuery<TFrom, T> : FlatQuery<TFrom, T>
    {
        public WhereQuery(Query<TFrom, T> parent, Expression<Func<T, bool>> expr)
            : base(parent)
        {
            ParentT = parent;
            WhereExpression = expr;
            WhereFunction = expr.Compile();
        }

        private Query<TFrom, T> ParentT { get; }
        private LambdaExpression WhereExpression { get; }
        private Func<T, bool> WhereFunction { get; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            return items.Where(x => WhereFunction(x));
        }

        internal override SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, parameters, out parentResult);

            var whereParameters = new Dictionary<string, SqlSubQueryResult>(parameters);
            whereParameters[WhereExpression.Parameters[0].Name] = parentResult;

            result.Wheres.Add(parser.ParseExpression(WhereExpression.Body, whereParameters));
            return result;
        }
    }
}
