using System;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlTableAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
