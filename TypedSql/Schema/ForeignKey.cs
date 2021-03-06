﻿using System;
using System.Collections.Generic;

namespace TypedSql.Schema
{
    public class ForeignKey
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public Type ReferenceTableType { get; set; }
        public List<string> ReferenceColumns { get; set; }
    }
}
