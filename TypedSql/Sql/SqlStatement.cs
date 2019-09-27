using System;
using System.Collections.Generic;
using System.Text;
using TypedSql.Schema;

namespace TypedSql
{
    /// <summary>
    /// Parsed SQL statement from IStatement
    /// </summary>
    public abstract class SqlStatement
    {
    }

    public class SqlCreateTable : SqlStatement
    {
        public string TableName { get; set; }
        public List<Schema.Column> Columns { get; set; }
    }

    public class SqlDropTable : SqlStatement
    {
        public string TableName { get; set; }
    }

    public class SqlDeclareVariable : SqlStatement
    {
        public string VariableName { get; set; }
        public Type VariableType { get; set; }
        public SqlTypeInfo SqlTypeInfo { get; set; }
    }

    public class SqlDelete : SqlStatement
    {
        public SqlQuery FromSource { get; set; }
    }

    public class SqlUpdate : SqlStatement
    {
        public SqlQuery FromSource { get; set; }
        public List<InsertInfo> Inserts { get; set; }
    }

    public class SqlInsert : SqlStatement
    {
        public string TableName { get; set; }
        public List<InsertInfo> Inserts { get; set; }
    }

    public class SqlInsertSelect : SqlStatement
    {
        public string TableName { get; set; }
        public List<InsertInfo> Inserts { get; set; }
        public SqlQuery FromSource { get; set; }
    }

    public class SqlSelect : SqlStatement
    {
        public SqlQuery FromSource { get; set; }
    }

    public class SqlSet : SqlStatement
    {
        public SqlPlaceholder Variable { get; set; }
        public SqlExpression Expression { get; set; }
    }

    public class SqlIf : SqlStatement
    {
        public SqlExpression Expression { get; set; }
        public List<SqlStatement> Block { get; set; }
        public List<SqlStatement> Block2 { get; set; }
    }
}
