using System;
using TypedSql.Schema;

namespace TypedSql
{
    public interface IDeclareVariableStatement : IStatement
    {
        string VariableName { get; }
        Type Type { get; }
        SqlTypeInfo SqlTypeInfo { get; set; }
    }

    public class DeclareVariableStatement<T> : IDeclareVariableStatement
    {
        public string VariableName { get; }
        public Type Type { get; } = typeof(T);
        public SqlTypeInfo SqlTypeInfo { get; set; }

        public DeclareVariableStatement(string variableName)
        {
            VariableName = variableName;
            SqlTypeInfo = new SqlTypeInfo();
        }

        public DeclareVariableStatement(string variableName, SqlTypeInfo sqlTypeInfo)
        {
            VariableName = variableName;
            SqlTypeInfo = sqlTypeInfo;
        }
    }
}
