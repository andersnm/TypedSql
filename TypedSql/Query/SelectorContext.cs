using System.Collections.Generic;

namespace TypedSql
{
    public class SelectorContext
    {
        public SelectorContext(IQueryRunner runner)
        {
            Runner = runner;
        }

        internal IQueryRunner Runner { get; }
    }

    public class SelectorContext<T> : SelectorContext
    {
        public SelectorContext(IQueryRunner runner, List<T> items)
            : base(runner)
        {
            Items = items;
        }

        internal List<T> Items { get; }
    }
}
