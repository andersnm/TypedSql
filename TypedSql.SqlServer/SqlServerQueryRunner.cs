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

        public override int ExecuteNonQuery(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine("BEGIN TRANSACTION");
            sb.AppendLine(GetSql(statements));
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
            }
            catch (SqlException e)
            {
                throw new InvalidOperationException(e.Message + " in " + sb.ToString());
            }
        }

        public override IEnumerable<T> ExecuteQuery<T>(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants)
        {
            using (var selectCommand = Connection.CreateCommand())
            {
                var sql = GetSql(statements);
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
                    foreach (var record in Materializer.ReadTypedReader<T>(reader, LastSelectMembers))
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
