using System;

namespace TypedSql
{
    public enum SqlPlaceholderType
    {
        None = 0,
        SessionVariableName,
    }

    public abstract class SqlPlaceholder
    {
        public string RawSql { get; set; }
        public abstract Type ValueType { get; }
        public SqlPlaceholderType PlaceholderType { get; set; }
    }

    public class SqlPlaceholder<T> : SqlPlaceholder
    {
        public override Type ValueType
        {
            get
            {
                return typeof(T);
            }
        }

        public T Value { get; set; }
    }
}
