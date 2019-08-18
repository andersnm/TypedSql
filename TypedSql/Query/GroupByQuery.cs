using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypedSql
{

    public interface IGroupByQuery
    {
        LambdaExpression GroupExpression { get; }
        LambdaExpression ProjectExpression { get; }
    }

    public class GroupByQuery<TFrom, T, TGroup, TProject> : AggregateQuery<TFrom, TProject>, IGroupByQuery
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
    }
}
