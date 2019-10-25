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

        // public static float Sum(float i) { return 0; }
        // public static decimal Sum(decimal i) { return 0; }
        // public static int Average(int i) { return 0; }
        // public static float Average(float i) { return 0; }
        // public static decimal Average(decimal i) { throw new InvalidOperationException("Cannot call aggregate directly"); }
        public static int Hour(DateTime t) { return t.Hour; }
        public static int Minute(DateTime t) { return t.Minute; }
        public static int Second(DateTime t) { return t.Second; }
        public static int? Second(DateTime? t) { return t?.Second; }
        public static int Year(DateTime t) { return t.Year; }
        public static int? Year(DateTime? t) { return t?.Year; }
        public static int Month(DateTime t) { return t.Month; }
        public static int Day(DateTime t) { return t.Day; }
    }
}
