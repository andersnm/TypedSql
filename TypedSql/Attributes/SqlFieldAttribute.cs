using System;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlFieldAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
