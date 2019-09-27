using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TypedSql
{
    public abstract class SqlQueryRunner : IQueryRunner
    {
        private CommonFormatter Formatter { get; }
        protected List<SqlMember> LastSelectMembers { get; set; }

        public SqlQueryRunner(CommonFormatter formatter)
        {
            Formatter = formatter;
        }

        public abstract int ExecuteNonQuery(SqlStatementList statementList);

        public abstract IEnumerable<T> ExecuteQuery<T>(SqlStatementList statementList);
        
        public IEnumerable<T> ExecuteQuery<T>(StatementResult<T> result)
        {
            return ExecuteQuery<T>(result.StatementList.RootScope);
        }

        public string GetSql(SqlStatementList statementList, out List<KeyValuePair<string, object>> constants)
        {
            var parser = new SqlQueryParser(new SqlAliasProvider(), new Dictionary<string, SqlSubQueryResult>());

            var stmts = parser.ParseStatementList(statementList);

            var sb = new StringBuilder();
            foreach (var stmt in stmts)
            {
                Formatter.WriteStatement(stmt, sb);
                if (stmt is SqlSelect select)
                {
                    LastSelectMembers = select.FromSource.SelectResult.Members;
                }
            }

            constants = parser.Constants.ToList();
            return sb.ToString();
        }
    }
}
