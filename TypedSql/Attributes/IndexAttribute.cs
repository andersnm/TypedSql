using System;
using System.Collections.Generic;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Class)]
    public class IndexAttribute : Attribute
    {
        public string Name { get; set; }

        // TODO: asc/desc pr column
        public string[] Columns { get; set; }
        public bool Unique { get; set; }
    }
}
