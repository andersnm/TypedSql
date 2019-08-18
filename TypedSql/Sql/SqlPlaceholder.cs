using System;

namespace TypedSql
{
    public enum SqlPlaceholderType
    {
        None = 0,
        RawSqlExpression,
        SessionVariableName,
    }

    public abstract class SqlPlaceholder
    {
        public string RawSql { get; set; }
        public abstract Type ValueType { get; }
        public SqlPlaceholderType PlaceholderType { get; set; }
    }

    public class SqlPlaceholder<T> : SqlPlaceholder where T : IComparable, IConvertible
    {
        public override Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }

        public T Value { get; set; }

        public static implicit operator SqlPlaceholder<T>(T value)
        {
            return new SqlPlaceholder<T>()
            {
                Value = value
            };
        }

        public static explicit operator T(SqlPlaceholder<T> value)
        {
            return value.Value;
        }
    }
}
