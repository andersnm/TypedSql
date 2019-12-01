using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace TypedSql.MySql
{
    public class MySqlQueryRunner : SqlQueryRunner
    {
        MySqlConnection Connection { get; set; }

        public MySqlQueryRunner(MySqlConnection connection)
            : base(new MySqlFormatter()) {
            Connection = connection;
        }

        public override int ExecuteNonQuery(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants)
        {
            using (var selectCommand = Connection.CreateCommand())
            {
                var sql = GetSql(statements);
                selectCommand.CommandText = sql;

                foreach (var parameter in constants)
                {
                    selectCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }

                try
                {
                    return selectCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(sql, e);
                }
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
