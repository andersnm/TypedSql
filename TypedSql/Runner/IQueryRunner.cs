using System.Collections.Generic;

namespace TypedSql
{
    public interface IQueryRunner
    {
        string GetSql(StatementList statementList, out List<KeyValuePair<string, object>> constants);
        IEnumerable<T> ExecuteQuery<T>(StatementList statementList);
        IEnumerable<T> ExecuteQuery<T>(StatementResult<T> statementList);
        int ExecuteNonQuery(StatementList statementList);
    }
}
