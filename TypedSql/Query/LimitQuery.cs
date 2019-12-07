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

        public Query<TFrom, T> ParentT { get; }
        public int LimitIndex { get; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            // Not limiting here, this is done in the select statement
            return items;
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);
            result.Limit = LimitIndex;
            return result;
        }
    }
}
