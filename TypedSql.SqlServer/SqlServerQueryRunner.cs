using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace TypedSql.SqlServer
{
    public class SqlServerQueryRunner : SqlQueryRunner
    {
        SqlConnection Connection { get; set; }

        public SqlServerQueryRunner(SqlConnection connection)
            :base(new SqlServerFormatter())
        {
            Connection = connection;
        }

        public override int ExecuteNonQuery(SqlStatementList statementList)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine("BEGIN TRANSACTION");
            sb.AppendLine(GetSql(statementList, out var constants));
            sb.AppendLine("COMMIT TRANSACTION");
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("  IF (@@TRANCOUNT > 0)");
            sb.AppendLine("    ROLLBACK TRANSACTION;");
            sb.AppendLine("  THROW;");
            sb.AppendLine("END CATCH");

            try
            {
                using (var selectCommand = Connection.CreateCommand())
                {
                    selectCommand.CommandText = sb.ToString();

                    foreach (var parameter in constants)
                    {
                        selectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    return selectCommand.ExecuteNonQuery();
                }
            } catch (SqlException e) {
                throw new InvalidOperationException(e.Message + " in " + sb.ToString());
            }
        }

        public override IEnumerable<T> ExecuteQuery<T>(SqlStatementList statementList)
        {
            return ExecuteQueryImpl<T>(statementList);
        }

        private IEnumerable<T> ExecuteQueryImpl<T>(SqlStatementList statementList)
        {
            using (var selectCommand = Connection.CreateCommand())
            {
                var sql = GetSql(statementList, out var constants);
                selectCommand.CommandText = sql;

                foreach (var parameter in constants)
                {
                    selectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }

                IDataReader reader;
                try
                {
                    reader = selectCommand.ExecuteReader();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(sql, e);
                }

                try
                {
                    foreach (var record in reader.ReadTypedReader<T>(LastSelectMembers))
                    {
                        yield return record;
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }
        }

    }
}
