﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypedSql
{
    public class GroupByQuery<TFrom, T, TGroup, TProject> : AggregateQuery<TFrom, TProject>
    {
        public Query<TFrom, T> ParentT { get; }
        public LambdaExpression GroupExpression { get; }
        public LambdaExpression ProjectExpression { get; }
        private Func<T, TGroup> GroupFunction { get; }
        private Func<SelectorContext<T>, T, TProject> ProjectFunction { get; }

        public GroupByQuery(FlatQuery<TFrom, T> parent, Expression<Func<T, TGroup>> groupExpr, Expression<Func<SelectorContext<T>, T, TProject>> projectExpr)
            : base(parent)
        {
            ParentT = parent;
            GroupExpression = groupExpr;
            GroupFunction = groupExpr.Compile();
            ProjectExpression = projectExpr;
            ProjectFunction = projectExpr.Compile();
        }

        internal override IEnumerable<TProject> InMemorySelect(IQueryRunner runner)
        {
            var groups = new Dictionary<TGroup, KeyValuePair<T, List<T>>>();
            // var groups = new Dictionary<TGroup, KeyValuePair<TProject, List<T>>>();
            foreach (var item in ParentT.InMemorySelect(runner))
            {
                var key = GroupFunction(item);
                if (!groups.ContainsKey(key))
                {
                    var listInstance = new List<T>();

                    groups[key] = new KeyValuePair<T, List<T>>(item, listInstance);
                    // groups[key] = new KeyValuePair<TProject, List<T>>(project, listInstance);
                }

                groups[key].Value.Add(item);
            }

            foreach (var key in groups.Keys)
            {
                var kv = groups[key];
                var context = new SelectorContext<T>(runner, kv.Value);
                var project = ProjectFunction(context, kv.Key);
                // FromRowMapping[kv.Key] = ParentT.FromRowMapping[];
                yield return project;
            }
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out var tempParentResult);

            var newExpression = GroupExpression.Body as NewExpression;

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[GroupExpression.Parameters[0].Name] = tempParentResult;

            foreach (var argument in newExpression.Arguments)
            {
                result.GroupBys.Add(parser.ParseExpression(argument, parameters));
            }

            var projectParameters = new Dictionary<string, SqlSubQueryResult>();
            projectParameters[ProjectExpression.Parameters[0].Name] = tempParentResult; // ctx
            projectParameters[ProjectExpression.Parameters[1].Name] = tempParentResult;

            parentResult = new SqlSubQueryResult()
            {
                Members = parser.ParseSelectExpression(ProjectExpression, projectParameters),
            };

            return result;
        }
    }
}
