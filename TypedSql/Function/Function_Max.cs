using System;

namespace TypedSql
{
    public static partial class Function
    {
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
    }
}
