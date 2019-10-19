using System;
using System.Collections.Generic;
using TypedSql.Schema;

namespace TypedSql
{
    public class SqlTable
    {
        public string TableName { get; set; }
        public List<SqlColumn> Columns { get; set; }
        public List<SqlForeignKey> ForeignKeys { get; set; }
        public List<SqlIndex> Indices { get; set; }
    }

    public class SqlColumn
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public bool PrimaryKeyAutoIncrement { get; set; }
        public SqlTypeInfo SqlType { get; set; }
    }

    public class SqlForeignKey
    {
        public string Name { get; set; }
        public string ReferenceTableName { get; set; }
        public List<string> Columns { get; set; }
        public List<string> ReferenceColumns { get; set; }
    }

    public class SqlIndex
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; }
    }

}
