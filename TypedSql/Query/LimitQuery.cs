using System.Collections.Generic;

namespace TypedSql
{
    public interface ILimitQuery
    {
        int LimitIndex { get; }
    }

    public class LimitQuery<TFrom, T> : FlatQuery<TFrom, T>, ILimitQuery
    {
        public LimitQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            ParentT = parent;
            LimitIndex = offset;
        }

        public int LimitIndex { get; }
        private Query<TFrom, T> ParentT { get; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            // Not limiting here, this is done in the select statement
            return items;
        }

        internal override SqlQuery Parse(SqlQueryParser parser, Dictionary<string, SqlSubQueryResult> parameters, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, parameters, out parentResult);
            result.Limit = LimitIndex;
            return result;
        }
    }
}
