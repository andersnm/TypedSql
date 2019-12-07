using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedSql.InMemory
{
    public class InMemoryQueryRunner : IQueryRunner
    {
        public InMemoryQueryRunner()
        {
        }

        public string GetSql(StatementList statementList, out List<KeyValuePair<string, object>> constants)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> ExecuteQuery<T>(StatementList statementList)
        {
            foreach (var query in statementList.Queries)
            {
                WriteStatement(query);
            }

            return lastResult.Cast<T>();
        }

        public IEnumerable<T> ExecuteQuery<T>(StatementResult<T> statementList)
        {
            foreach (var query in statementList.StatementList.RootScope.Queries)
            {
                WriteStatement(query);
            }

            return lastResult.Cast<T>();
        }

        public int ExecuteNonQuery(StatementList statementList)
        {
            affectedRows = 0;
            foreach (var query in statementList.Queries)
            {
                WriteStatement(query);
            }

            return affectedRows;
        }

        private List<object> lastResult;
        private int affectedRows;
        public object LastIdentity { get; private set; }

        private void WriteStatement(IStatement stmt)
        {
            switch (stmt)
            {
                case IInsertStatement insertStmt:
                    WriteInsertStatement(insertStmt);
                    break;
                case IInsertSelectStatement insertSelectStmt:
                    WriteInsertSelectStatement(insertSelectStmt);
                    break;
                case ISelectStatement selectStmt:
                    WriteSelectStatement(selectStmt);
                    break;
                case IUpdateStatement updateStmt:
                    WriteUpdateStatement(updateStmt);
                    break;
                case IDeleteStatement deleteStmt:
                    WriteDeleteStatement(deleteStmt);
                    break;
                case IDeclareVariableStatement declareStmt:
                    WriteDeclareSqlVariable(declareStmt);
                    break;
                case ISetVariableStatement setStmt:
                    WriteSetSqlVariable(setStmt);
                    break;
                default:
                    throw new Exception("Unsupported statement " + stmt.GetType().Name);
            }
        }

        private void WriteSelectStatement(ISelectStatement stmt)
        {
            lastResult = stmt.EvaluateInMemory(this);
            affectedRows = lastResult.Count;
        }

        private void WriteInsertStatement(IInsertStatement stmt)
        {
            var insertedRows = stmt.EvaluateInMemory(this, out var identity);
            if (insertedRows > 0)
            {
                affectedRows += insertedRows;
                LastIdentity = identity;
            }
        }

        private void WriteInsertSelectStatement(IInsertSelectStatement stmt)
        {
            var insertedRows = stmt.EvaluateInMemory(this, out var identity);
            if (insertedRows > 0)
            {
                affectedRows += insertedRows;
                LastIdentity = identity;
            }
        }

        private void WriteUpdateStatement(IUpdateStatement stmt)
        {
            affectedRows += stmt.EvaluateInMemory(this);
        }

        private void WriteDeleteStatement(IDeleteStatement stmt)
        {
            affectedRows += stmt.EvaluateInMemory(this);
        }

        private void WriteDeclareSqlVariable(IDeclareVariableStatement stmt)
        {
        }

        private void WriteSetSqlVariable(ISetVariableStatement stmt)
        {
            stmt.EvaluateInMemory(this);
        }
    }
}
