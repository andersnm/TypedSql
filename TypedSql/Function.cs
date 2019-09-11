using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TypedSql.InMemory;

namespace TypedSql {
    public static partial class Function {

        public static int Count<T, TX>(SelectorContext<T> t, Func<T, TX> selector)
        {
            return t.Items.Count;
        }

        public static byte? Sum<T>(SelectorContext<T> t, Func<T, byte?> selector)
        {
            var result = (byte?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static short? Sum<T>(SelectorContext<T> t, Func<T, short?> selector)
        {
            var result = (short?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static int? Sum<T>(SelectorContext<T> t, Func<T, int?> selector)
        {
            var result = (int?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static long? Sum<T>(SelectorContext<T> t, Func<T, long?> selector)
        {
            var result = (long?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static decimal? Sum<T>(SelectorContext<T> t, Func<T, decimal?> selector)
        {
            var result = (decimal?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static float? Sum<T>(SelectorContext<T> t, Func<T, float?> selector)
        {
            var result = (float?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static double? Sum<T>(SelectorContext<T> t, Func<T, double?> selector)
        {
            var result = (double?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static byte? Average<T>(SelectorContext<T> t, Func<T, byte?> selector)
        {
            var result = (byte?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return Convert.ToByte(result.Value / count);
        }

        public static short? Average<T>(SelectorContext<T> t, Func<T, short?> selector)
        {
            var result = (short?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return Convert.ToByte(result.Value / count);
        }

        public static int? Average<T>(SelectorContext<T> t, Func<T, int?> selector)
        {
            var result = (int?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return result / count;
        }

        public static long? Average<T>(SelectorContext<T> t, Func<T, long?> selector)
        {
            var result = (long?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return result / count;
        }

        public static decimal? Average<T>(SelectorContext<T> t, Func<T, decimal?> selector)
        {
            var result = (decimal?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return result / count;
        }

        public static float? Average<T>(SelectorContext<T> t, Func<T, float?> selector)
        {
            var result = (float?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return result / count;
        }

        public static double? Average<T>(SelectorContext<T> t, Func<T, double?> selector)
        {
            var result = (double?)0;
            var count = 0;
            foreach (var item in t.Items)
            {
                result += selector(item);
                count++;
            }

            return result / count;
        }

        public static byte? Min<T>(SelectorContext<T> t, Func<T, byte?> selector)
        {
            var result = (byte?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static short? Min<T>(SelectorContext<T> t, Func<T, short?> selector)
        {
            var result = (short?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static int? Min<T>(SelectorContext<T> t, Func<T, int?> selector)
        {
            var result = (int?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static long? Min<T>(SelectorContext<T> t, Func<T, long?> selector)
        {
            var result = (long?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static decimal? Min<T>(SelectorContext<T> t, Func<T, decimal?> selector)
        {
            var result = (decimal?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static float? Min<T>(SelectorContext<T> t, Func<T, float?> selector)
        {
            var result = (float?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static double? Min<T>(SelectorContext<T> t, Func<T, double?> selector)
        {
            var result = (double?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Min(result.Value, selector(item).Value);
                }
            }

            return result;
        }
        public static byte? Max<T>(SelectorContext<T> t, Func<T, byte?> selector)
        {
            var result = (byte?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static short? Max<T>(SelectorContext<T> t, Func<T, short?> selector)
        {
            var result = (short?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static int? Max<T>(SelectorContext<T> t, Func<T, int?> selector)
        {
            var result = (int?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static long? Max<T>(SelectorContext<T> t, Func<T, long?> selector)
        {
            var result = (long?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static decimal? Max<T>(SelectorContext<T> t, Func<T, decimal?> selector)
        {
            var result = (decimal?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static float? Max<T>(SelectorContext<T> t, Func<T, float?> selector)
        {
            var result = (float?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
        }

        public static double? Max<T>(SelectorContext<T> t, Func<T, double?> selector)
        {
            var result = (double?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    result = Math.Max(result.Value, selector(item).Value);
                }
            }

            return result;
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

        public static T LastInsertIdentity<T>(SelectorContext<T> context)
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
