using System;
using System.Collections.Generic;

namespace TypedSql.Schema
{
    public class Index
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
        public bool Unique { get; set; }
    }
}
