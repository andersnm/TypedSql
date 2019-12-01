using System;

namespace TypedSql
{
    public static partial class Function
    {
        public static int? Sum<T>(SelectorContext<T> t, Func<T, byte?> selector)
        {
            var result = (int?)0;
            foreach (var item in t.Items)
            {
                result += selector(item);
            }

            return result;
        }

        public static int? Sum<T>(SelectorContext<T> t, Func<T, short?> selector)
        {
            var result = (int?)0;
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

        public static double? Sum<T>(SelectorContext<T> t, Func<T, float?> selector)
        {
            var result = (double?)0;
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
    }
}
