using System;
using System.Collections.Generic;
using System.Text;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKeyAttribute : Attribute
    {
        public bool AutoIncrement { get; set; }
    }
}
