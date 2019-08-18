using System.Collections.Generic;

namespace TypedSql
{
    public interface IQueryRunner
    {
        string GetSql(SqlStatementList statementList, out List<KeyValuePair<string, object>> constants);
        IEnumerable<T> ExecuteQuery<T>(SqlStatementList statementList);
        IEnumerable<T> ExecuteQuery<T>(StatementResult<T> statementList);
        int ExecuteNonQuery(SqlStatementList statementList);
    }
}
