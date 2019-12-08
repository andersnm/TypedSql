using System;
using TypedSql.Schema;

namespace TypedSql
{
    public interface IDeclareVariableStatement : IStatement
    {
    }

    public class DeclareVariableStatement<T> : IDeclareVariableStatement
    {
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

        private string VariableName { get; }
        private SqlTypeInfo SqlTypeInfo { get; set; }

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
