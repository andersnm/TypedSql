﻿using System;
using System.Collections.Generic;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ForeignKeyAttribute : Attribute
    {
        public string Name { get; set; }

        public Type ReferenceTableType { get; set; }

        public string[] ReferenceColumns { get; set; }

        public string[] Columns { get; set; }
    }
}
