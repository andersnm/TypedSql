using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypedSql
{
    public abstract class SqlQueryRunner : IQueryRunner
    {
        private IFormatter Formatter { get; }
        protected List<SqlMember> LastSelectMembers { get; set; }

        public SqlQueryRunner(IFormatter formatter)
        {
            Formatter = formatter;
        }

        public abstract int ExecuteNonQuery(SqlStatementList statementList);

        public abstract IEnumerable<T> ExecuteQuery<T>(SqlStatementList statementList);
        
        public IEnumerable<T> ExecuteQuery<T>(StatementResult<T> result)
        {
            return ExecuteQuery<T>(result.StatementList);
        }

        public string GetSql(SqlStatementList statementList, out List<KeyValuePair<string, object>> constants)
        {
            var sb = new StringBuilder();
            var parser = new SqlQueryParser(new SqlAliasProvider(), new Dictionary<string, SqlSubQueryResult>());
            foreach (var query in statementList.Queries)
            {
                WriteStatement(query, parser, Formatter, sb);
            }

            constants = parser.Constants.ToList();
            return sb.ToString();
        }

        void WriteStatement(IStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            switch (stmt)
            {
                case IInsertStatement insertStmt:
                    WriteInsertStatement(insertStmt, parser, formatter, sb);
                    return;
                case IInsertSelectStatement insertSelectStmt:
                    WriteInsertSelectStatement(insertSelectStmt, parser, formatter, sb);
                    return;
                case ISelectStatement selectStmt:
                    WriteSelectStatement(selectStmt, parser, formatter, sb);
                    return;
                case IUpdateStatement updateStmt:
                    WriteUpdateStatement(updateStmt, parser, formatter, sb);
                    return;
                case IDeleteStatement deleteStmt:
                    WriteDeleteStatement(deleteStmt, parser, formatter, sb);
                    return;
                case IDeclareVariableStatement declareStmt:
                    formatter.WriteDeclareSqlVariable(declareStmt.VariableName, declareStmt.Type, sb);
                    return;
                case ISetVariableStatement setStmt:
                    formatter.WriteSetSqlVariable(setStmt.Variable, parser.ParseExpression(setStmt.ValueExpression), sb);
                    return;
                case ICreateTableStatement createTableStatement:
                    WriteCreateTableStatement(createTableStatement.Table, formatter, sb);
                    return;
                case IDropTableStatement dropTableStatement:
                    WriteDropTableStatement(dropTableStatement.Table, formatter, sb);
                    return;
            }

            throw new Exception("Unsupported statement " + stmt.GetType().Name);
        }

        void WriteSelectStatement(ISelectStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            var subqr = parser.ParseQuery(stmt.SelectQuery);
            formatter.WriteSelectQuery(subqr, sb);
            sb.AppendLine(";");
            LastSelectMembers = subqr.SelectResult.Members;
        }

        void WriteInsertStatement(IInsertStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            var inserts = parser.ParseInsertBuilder(stmt.FromQuery, stmt.InsertExpression, parameters);
            formatter.WriteInsertBuilderQuery(inserts, stmt.FromQuery, sb);
        }

        void WriteInsertSelectStatement(IInsertSelectStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            var parentQueryResult = parser.ParseQuery(stmt.SelectQuery);

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[stmt.InsertExpression.Parameters[0].Name] = parentQueryResult.SelectResult;

            var inserts = parser.ParseInsertBuilder(stmt.FromQuery, stmt.InsertExpression, parameters);
            formatter.WriteInsertBuilderQuery(parentQueryResult, inserts, stmt.FromQuery, sb);
        }

        void WriteUpdateStatement(IUpdateStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            var queryResult = parser.ParseQuery(stmt.Parent);
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[stmt.InsertExpression.Parameters[0].Name] = queryResult.SelectResult; // item
            // parameters[stmt.InsertExpression.Parameters[1].Name] = ; // builder

            var inserts = parser.ParseInsertBuilder(stmt.FromQuery, stmt.InsertExpression, parameters);
            formatter.WriteUpdateQuery(inserts, queryResult, sb);
            sb.AppendLine(";");
        }

        void WriteDeleteStatement(IDeleteStatement stmt, SqlQueryParser parser, IFormatter formatter, StringBuilder sb)
        {
            var queryResult = parser.ParseQuery(stmt.Parent);
            formatter.WriteDeleteQuery(queryResult, sb);
            sb.AppendLine(";");
        }

        void WriteCreateTableStatement(IFromQuery table, IFormatter formatter, StringBuilder sb)
        {
            CreateTableSql(table, formatter, sb);
            sb.AppendLine(";");
        }

        void WriteDropTableStatement(IFromQuery table, IFormatter formatter, StringBuilder sb)
        {
            DropTableSql(table, true, formatter, sb);
            sb.AppendLine(";");
        }

        void CreateTableSql(IFromQuery table, IFormatter formatter, StringBuilder writer)
        {
            writer.Append("CREATE TABLE ");
            formatter.WriteTableName(table.TableName, writer);
            writer.Append(" (");

            for (var i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                if (i > 0)
                {
                    writer.Append(", ");
                }

                formatter.WriteCreateTableColumn(column, writer);
            }

            writer.Append(")");
        }

        void DropTableSql(IFromQuery table, bool useIfExists, IFormatter formatter, StringBuilder writer)
        {
            // TODO: override for sql server <2016
            writer.Append("DROP TABLE ");
            if (useIfExists)
            {
                writer.Append("IF EXISTS ");
            }

            formatter.WriteTableName(table.TableName, writer);
        }
    }
}
