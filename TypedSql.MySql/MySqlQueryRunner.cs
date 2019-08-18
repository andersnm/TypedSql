using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TypedSql.MySql
{
    public class MySqlQueryRunner : SqlQueryRunner
    {
        MySqlConnection Connection { get; set; }

        public MySqlQueryRunner(MySqlConnection connection)
            : base(new MySqlFormatter()) {
            Connection = connection;
        }

        public override int ExecuteNonQuery(SqlStatementList statementList)
        {
            using (var selectCommand = Connection.CreateCommand())
            {
                var sql = GetSql(statementList, out var constants);
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
                    foreach (var record in reader.ReadTypedReader<T>())
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
