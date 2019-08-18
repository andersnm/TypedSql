﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedSql.InMemory
{
    public class InMemoryQueryRunner : IQueryRunner
    {
        public InMemoryQueryRunner()
        {
        }

        public string GetSql(SqlStatementList statementList, out List<KeyValuePair<string, object>> constants)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> ExecuteQuery<T>(SqlStatementList statementList)
        {
            foreach (var query in statementList.Queries)
            {
                WriteStatement(query);
            }

            return lastResult.Cast<T>();
        }

        public IEnumerable<T> ExecuteQuery<T>(StatementResult<T> statementList)
        {
            foreach (var query in statementList.StatementList.Queries)
            {
                WriteStatement(query);
            }

            return lastResult.Cast<T>();
        }

        public int ExecuteNonQuery(SqlStatementList statementList)
        {
            foreach (var query in statementList.Queries)
            {
                WriteStatement(query);
            }

            return lastStatementResult;
        }


        List<object> lastResult;
        int lastStatementResult;
        public object LastIdentity { get; private set; }

        void WriteStatement(IStatement stmt)
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
                case ICreateTableStatement createTableStatement:
                    WriteCreateTableStatement(createTableStatement);
                    break;
                case IDropTableStatement dropTableStatement:
                    WriteDropTableStatement(dropTableStatement);
                    break;
            }
        }

        void WriteSelectStatement(ISelectStatement stmt)
        {
            lastResult = stmt.EvaluateInMemory(this);
            lastStatementResult = lastResult.Count;
        }

        void WriteInsertStatement(IInsertStatement stmt)
        {
            lastStatementResult = stmt.EvaluateInMemory(this, out var identity);
            if (lastStatementResult > 0)
            {
                LastIdentity = identity;
            }
        }

        void WriteInsertSelectStatement(IInsertSelectStatement stmt)
        {
            lastStatementResult = stmt.EvaluateInMemory(this, out var identity);
            if (lastStatementResult > 0)
            {
                LastIdentity = identity;
            }
        }

        void WriteUpdateStatement(IUpdateStatement stmt)
        {
            lastStatementResult = stmt.EvaluateInMemory(this);
        }

        void WriteDeleteStatement(IDeleteStatement stmt)
        {
            lastStatementResult = stmt.EvaluateInMemory(this);
        }

        void WriteDeclareSqlVariable(IDeclareVariableStatement stmt)
        {
        }

        void WriteSetSqlVariable(ISetVariableStatement stmt)
        {
            stmt.EvaluateInMemory(this);
        }

        void WriteCreateTableStatement(ICreateTableStatement createTableStatement)
        {
            lastStatementResult = 1;
        }

        void WriteDropTableStatement(IDropTableStatement dropTableStatement)
        {
            lastStatementResult = 1;
        }

    }
}