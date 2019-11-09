using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypedSql
{
    public abstract class SqlQueryRunner : IQueryRunner
    {
        protected SqlBaseFormatter Formatter { get; }
        protected List<SqlMember> LastSelectMembers { get; set; }

        public SqlQueryRunner(SqlBaseFormatter formatter)
        {
            Formatter = formatter;
        }

        public abstract int ExecuteNonQuery(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants);
        public abstract IEnumerable<T> ExecuteQuery<T>(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants);

        public int ExecuteNonQuery(StatementList statementList)
        {
            var parser = new SqlQueryParser(new SqlAliasProvider(), new Dictionary<string, SqlSubQueryResult>());
            var stmts = parser.ParseStatementList(statementList);
            var constants = parser.Constants.ToList();
            return ExecuteNonQuery(stmts, constants);
        }

        public IEnumerable<T> ExecuteQuery<T>(StatementList statementList)
        {
            var parser = new SqlQueryParser(new SqlAliasProvider(), new Dictionary<string, SqlSubQueryResult>());
            var stmts = parser.ParseStatementList(statementList);
            var constants = parser.Constants.ToList();
            return ExecuteQuery<T>(stmts, constants);
        }

        public IEnumerable<T> ExecuteQuery<T>(StatementResult<T> result)
        {
            return ExecuteQuery<T>(result.StatementList.RootScope);
        }

        public string GetSql(StatementList statementList, out List<KeyValuePair<string, object>> constants)
        {
            var parser = new SqlQueryParser(new SqlAliasProvider(), new Dictionary<string, SqlSubQueryResult>());
            var stmts = parser.ParseStatementList(statementList);
            constants = parser.Constants.ToList();
            return GetSql(stmts);
        }

        public string GetSql(List<SqlStatement> stmts)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < stmts.Count; i++)
            {
                var stmt = stmts[i];
                Formatter.WriteStatement(stmt, i == stmts.Count - 1, sb);
                if (stmt is SqlSelect select)
                {
                    LastSelectMembers = select.FromSource.SelectResult.Members;
                }
            }

            return sb.ToString();
        }
    }
}
