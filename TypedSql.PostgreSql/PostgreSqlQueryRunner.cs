using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using Npgsql;
using TypedSql.Schema;

namespace TypedSql.PostgreSql
{
    public class PostgreSqlQueryRunner : SqlQueryRunner
    {
        NpgsqlConnection Connection { get; set; }

        public PostgreSqlQueryRunner(NpgsqlConnection connection)
            : base(new PostgreSqlFormatter()) {
            Connection = connection;
        }

        public override int ExecuteNonQuery(List<SqlStatement> statements, List<KeyValuePair<string, object>> constants)
        {
            var sql = GetSqlWithProlog(statements);

            using (var selectCommand = Connection.CreateCommand())
            {
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
            var sql = GetSqlWithProlog(statements);

            using (var selectCommand = Connection.CreateCommand())
            {
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

        private string GetSqlWithProlog(List<SqlStatement> statements)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DROP TABLE IF EXISTS \"_typedsql_variables\";");
            sb.AppendLine("CREATE TEMPORARY TABLE \"_typedsql_variables\" AS");
            sb.AppendLine("SELECT CAST(NULL AS INT) AS _typedsql_last_insert_identity");

            foreach (var stmt in statements)
            {
                if (stmt is SqlDeclareVariable declareVariable)
                {
                    var propertyTypeInfo = declareVariable.VariableType.GetTypeInfo();
                    var nullable = (propertyTypeInfo.IsGenericType && propertyTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>));
                    var baseType = nullable ? Nullable.GetUnderlyingType(declareVariable.VariableType) : declareVariable.VariableType;

                    sb.Append(", CAST(NULL AS ");
                    Formatter.WriteColumnType(baseType, declareVariable.SqlTypeInfo, sb);
                    sb.Append(") AS ");
                    Formatter.WriteColumnName(declareVariable.VariableName, sb);
                }
            }
            sb.AppendLine(";");

            sb.AppendLine(GetSql(statements));

            return sb.ToString();
        }
    }
}
