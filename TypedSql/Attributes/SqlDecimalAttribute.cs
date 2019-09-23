using System;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlDecimalAttribute : Attribute
    {
        public int Precision { get; set; }
        public int Scale { get; set; }
    }
}
