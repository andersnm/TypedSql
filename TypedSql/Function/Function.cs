using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TypedSql.InMemory;

namespace TypedSql
{
    public static partial class Function
    {
        public static int Count<T, TX>(SelectorContext<T> t, Func<T, TX> selector)
        {
            return t.Items.Count;
        }

        public static bool Like(string lhs, string rhs)
        {
            // https://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-like
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(rhs, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(lhs);
        }

        public static bool Contains<T, TResult>(SelectorContext<T> context, TResult value, SelectStatement<TResult> sq)
        {
            return sq.SelectQueryTResult.InMemorySelect((InMemoryQueryRunner)context.Runner).Contains(value);
        }

        public static bool Contains<T>(T value, IEnumerable<T> sq)
        {
            return sq.Contains(value);
        }

        public static T LastInsertIdentity<T>(SelectorContext context)
        {
            return (T)((InMemoryQueryRunner)context.Runner).LastIdentity;
        }

        public static int Hour(DateTime t) => t.Hour;

        public static int? Hour(DateTime? t) => t?.Hour;

        public static int Minute(DateTime t) => t.Minute;

        public static int? Minute(DateTime? t) => t?.Minute;

        public static int Second(DateTime t) => t.Second;

        public static int? Second(DateTime? t) => t?.Second;

        public static int Year(DateTime t) => t.Year;

        public static int? Year(DateTime? t) => t?.Year;

        public static int Month(DateTime t) => t.Month;

        public static int? Month(DateTime? t) => t?.Month;

        public static int Day(DateTime t) => t.Day;

        public static int? Day(DateTime? t) => t?.Day;
    }
}
