using System;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlStringAttribute : Attribute
    {
        public int Length { get; set; }
        public bool NVarChar { get; set; }
    }
}
