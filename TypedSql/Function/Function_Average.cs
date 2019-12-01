using System;

namespace TypedSql
{
    public static partial class Function
    {
        public static int? Average<T>(SelectorContext<T> t, Func<T, byte?> selector)
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

        public static int? Average<T>(SelectorContext<T> t, Func<T, short?> selector)
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

        public static double? Average<T>(SelectorContext<T> t, Func<T, float?> selector)
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
    }
}
