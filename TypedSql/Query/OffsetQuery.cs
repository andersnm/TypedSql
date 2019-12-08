using System.Collections.Generic;

namespace TypedSql
{
    public interface IOffsetQuery
    {
        int OffsetIndex { get; }
    }

    public class OffsetQuery<TFrom, T> : FlatQuery<TFrom, T>, IOffsetQuery
    {
        public OffsetQuery(Query<TFrom, T> parent, int offset)
            : base(parent)
        {
            ParentT = parent;
            OffsetIndex = offset;
        }

        public int OffsetIndex { get; }
        private Query<TFrom, T> ParentT { get; }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            var items = ParentT.InMemorySelect(runner);
            FromRowMapping = ParentT.FromRowMapping;
            // Not offsetting here, this is done in the select statement
            return items;
        }

        internal override SqlQuery Parse(SqlQueryParser parser, out SqlSubQueryResult parentResult)
        {
            var result = ParentT.Parse(parser, out parentResult);
            result.Offset = OffsetIndex;
            return result;
        }
    }
}
