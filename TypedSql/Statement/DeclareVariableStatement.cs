using System;

namespace TypedSql
{
    public interface IDeclareVariableStatement : IStatement
    {
        string VariableName { get; }
        Type Type { get; }
    }

    public class DeclareVariableStatement<T> : IDeclareVariableStatement
    {
        public string VariableName { get; }
        public Type Type { get; } = typeof(T);

        public DeclareVariableStatement(string variableName)
        {
            VariableName = variableName;
        }
    }
}
