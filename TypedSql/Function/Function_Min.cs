using System;

namespace TypedSql
{
    public static partial class Function
    {
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

        public static DateTime? Min<T>(SelectorContext<T> t, Func<T, DateTime?> selector)
        {
            var result = (DateTime?)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (!result.HasValue)
                {
                    result = value;
                }
                else if (value.HasValue)
                {
                    var selectorValue = selector(item).Value;
                    result = result.Value < selectorValue ? result.Value : selectorValue;
                }
            }

            return result;
        }

        public static string Min<T>(SelectorContext<T> t, Func<T, string> selector)
        {
            var result = (string)null;
            foreach (var item in t.Items)
            {
                var value = selector(item);
                if (result == null)
                {
                    result = value;
                }
                else if (value != null)
                {
                    var selectorValue = selector(item);
                    result = result.CompareTo(selectorValue) < 0 ? result : selectorValue;
                }
            }

            return result;
        }
    }
}
