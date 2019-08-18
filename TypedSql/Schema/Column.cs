using System;
using System.Reflection;

namespace TypedSql.Schema
{
    public class Column
    {
        public string MemberName { get; set; }
        public string SqlName { get; set; }
        public Type OriginalType { get; set; }
        public Type BaseType { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public bool PrimaryKeyAutoIncrement { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
    }
}
