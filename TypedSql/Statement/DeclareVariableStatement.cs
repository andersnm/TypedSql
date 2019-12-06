using System;
using TypedSql.Schema;

namespace TypedSql
{
    public interface IDeclareVariableStatement : IStatement
    {
    }

    public class DeclareVariableStatement<T> : IDeclareVariableStatement
    {
        public string VariableName { get; }
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

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlDeclareVariable()
            {
                VariableName = VariableName,
                VariableType = typeof(T),
                SqlTypeInfo = SqlTypeInfo,
            };
        }
    }
}
