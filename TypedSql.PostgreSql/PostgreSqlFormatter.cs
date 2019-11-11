using System;
using System.Collections.Generic;
using System.Text;
using TypedSql.Schema;

namespace TypedSql.PostgreSql
{
    public class PostgreSqlFormatter : SqlBaseFormatter
    {
        public override void WriteColumnName(string columnName, StringBuilder writer)
        {
            writer.Append("\"");
            writer.Append(columnName);
            writer.Append("\"");
        }

        public override void WriteTableName(string tableName, StringBuilder writer)
        {
            writer.Append("\"");
            writer.Append(tableName);
            writer.Append("\"");
        }

        public override string WriteColumnType(Type type, SqlTypeInfo sqlTypeInfo)
        {
            // https://www.postgresql.org/docs/9.1/datatype.html
            // https://www.npgsql.org/doc/types/basic.html
            if (type == typeof(sbyte))
            {
                throw new InvalidOperationException("Signed byte is not supported in PostgreSQL");
            }
            else if (type == typeof(byte))
            {
                // NOTE: Returns type of larger size!
                // throw new InvalidOperationException("Unsigned byte is not supported in PostgreSQL");
                return "SMALLINT";
            }
            else if (type == typeof(short))
            {
                return "SMALLINT";
            }
            else if (type == typeof(ushort))
            {
                throw new InvalidOperationException("Unsigned short is not supported in PostgreSQL");
            }
            else if (type == typeof(int))
            {
                return "INT";
            }
            else if (type == typeof(uint))
            {
                throw new InvalidOperationException("Unsigned int is not supported in PostgreSQL");
            }
            else if (type == typeof(long))
            {
                return "BIGINT";
            }
            else if (type == typeof(ulong))
            {
                throw new InvalidOperationException("Unsigned long is not supported in PostgreSQL");
            }
            else if (type == typeof(decimal))
            {
                return $"DECIMAL({sqlTypeInfo.DecimalPrecision}, {sqlTypeInfo.DecimalScale})";
            }
            else if (type == typeof(float))
            {
                return "REAL";
            }
            else if (type == typeof(double))
            {
                return "DOUBLE PRECISION";
            }
            else if (type == typeof(string))
            {
                if (sqlTypeInfo.StringLength > 0)
                {
                    return $"VARCHAR({sqlTypeInfo.StringLength})";
                }
                else
                {
                    return "VARCHAR";
                }
            }
            else if (type == typeof(DateTime))
            {
                return "TIMESTAMP";
            }
            else if (type == typeof(bool))
            {
                return "BOOLEAN";
            }
            else if (type == typeof(byte[]))
            {
                return "BYTEA";
            }
            else
            {
                throw new InvalidOperationException("FormatType unsupported type " + type.ToString());
            }
        }

        public override void WriteCreateTableColumn(SqlColumn column, StringBuilder writer)
        {
            WriteColumnName(column.Name, writer);
            writer.Append(" ");

            // Primary keys are implicit NOT NULL
            if (column.PrimaryKey)
            {
                // TODO: BIGSERIAL
                if (column.Type != typeof(int))
                {
                    throw new InvalidOperationException("PostgreSQL expected int primary key");
                }

                if (column.PrimaryKeyAutoIncrement)
                {
                    writer.Append(" SERIAL");
                }

                writer.Append(" PRIMARY KEY");
            }
            else
            {
                writer.Append(WriteColumnType(column.Type, column.SqlType));

                if (column.Nullable)
                {
                    writer.Append(" NULL");
                }
                else
                {
                    writer.Append(" NOT NULL");
                }
            }

        }

        public override void WriteDeclareSqlVariable(string name, Type type, SqlTypeInfo sqlTypeInfo, StringBuilder writer)
        {
            // OK: No-op
        }

        public override void WriteSetSqlVariable(SqlPlaceholder variable, SqlExpression expr, StringBuilder writer)
        {
            writer.Append("UPDATE \"_typedsql_variables\" SET ");
            if (variable.PlaceholderType == SqlPlaceholderType.SessionVariableName)
            {
                WriteColumnName(variable.RawSql, writer);
            }
            else
            {
                throw new InvalidOperationException("Unsupported placeholder in variable assignment " + variable.PlaceholderType);
            }

            writer.Append(" = ");
            WriteExpression(expr, writer);
            writer.AppendLine(";");
        }

        public override void WritePlaceholder(SqlPlaceholder placeholder, StringBuilder writer)
        {
            if (placeholder.PlaceholderType == SqlPlaceholderType.SessionVariableName)
            {
                writer.Append("(SELECT ");
                WriteColumnName(placeholder.RawSql, writer);
                writer.Append(" FROM \"_typedsql_variables\")");
            }
            else
            {
                throw new InvalidOperationException("Unsupported placeholder " + placeholder.PlaceholderType);
            }
        }

        public override void WriteLastIdentityExpression(StringBuilder writer)
        {
            writer.Append("(SELECT \"_typedsql_last_insert_identity\" FROM \"_typedsql_variables\")");
        }

        public override void WriteIfNullExpression(SqlExpression testExpr, SqlExpression ifNullExpr, StringBuilder writer)
        {
            writer.Append("COALESCE(");
            WriteExpression(testExpr, writer);
            writer.Append(", ");
            WriteExpression(ifNullExpr, writer);
            writer.Append(")");
        }

        public override void WriteModuloExpression(SqlExpression leftExpr, SqlExpression rightExpr, StringBuilder writer)
        {
            writer.Append("MOD(");
            WriteExpression(leftExpr, writer);
            writer.Append(", ");
            WriteExpression(rightExpr, writer);
            writer.Append(")");
        }

        public override void WriteSelectQuery(SqlQuery queryObject, StringBuilder writer)
        {
            base.WriteSelectQuery(queryObject, writer);

            if (queryObject.Limit.HasValue)
            {
                writer.Append(" LIMIT ");
                writer.Append(queryObject.Limit.Value);
            }

            if (queryObject.Offset.HasValue)
            {
                writer.Append(" OFFSET ");
                writer.Append(queryObject.Offset.Value);
            }
        }

        public override void WriteUpdateQuery(List<InsertInfo> inserts, SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("UPDATE ");
            WriteFromSource(queryObject.From, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);

            writer.Append(" SET ");

            for (var i = 0; i < inserts.Count; i++)
            {
                var insert = inserts[i];

                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteColumnName(insert.SqlName, writer);
                writer.Append(" = ");
                WriteExpression(insert.Expression, writer);
            }

            // PGSQL UPDATE FROM joined tables, similar to DELETE USING
            var wheres = new List<SqlExpression>();
            if (queryObject.Joins.Count > 0)
            {
                writer.Append(" FROM ");
                for (var i = 0; i < queryObject.Joins.Count; i++)
                {
                    var join = queryObject.Joins[i];
                    if (join.JoinType != JoinType.InnerJoin)
                    {
                        throw new InvalidOperationException("PostgreSQL supports only inner joins in UPDATE statements");
                    }

                    if (!(join is SqlJoinTable joinTable))
                    {
                        throw new InvalidOperationException("PostgreSQL supports only joining tables in UPDATE statements");
                    }

                    if (i > 0)
                    {
                        writer.Append(", ");
                    }

                    WriteFromSource(joinTable.FromSource, writer);
                    writer.Append(" ");
                    writer.Append(joinTable.TableAlias);

                    wheres.Add(joinTable.JoinExpression);
                }
            }

            wheres.AddRange(queryObject.Wheres);

            if (wheres.Count > 0)
            {
                writer.Append("\nWHERE ");
                for (var i = 0; i < wheres.Count; i++)
                {
                    var criteria = wheres[i];
                    if (i > 0)
                    {
                        writer.AppendLine(" AND");
                    }

                    WriteExpression(criteria, writer);
                }
            }
            // WriteFinalQuery(queryObject, writer);

        }

        public override void WriteDeleteQuery(SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("DELETE FROM ");
            WriteFromSource(queryObject.From, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);
            writer.Append(" ");

            // PGSQL DELETE USING joined tables, similar to UPDATE FROM
            var wheres = new List<SqlExpression>();
            if (queryObject.Joins.Count > 0)
            {
                writer.Append("USING ");
                for (var i = 0; i < queryObject.Joins.Count; i++)
                {
                    var join = queryObject.Joins[i];
                    if (join.JoinType != JoinType.InnerJoin)
                    {
                        throw new InvalidOperationException("PostgreSQL supports only inner joins in DELETE statements");
                    }

                    if (!(join is SqlJoinTable joinTable))
                    {
                        throw new InvalidOperationException("PostgreSQL supports only joining tables in DELETE statements");
                    }

                    if (i > 0)
                    {
                        writer.Append(", ");
                    }

                    WriteFromSource(joinTable.FromSource, writer);
                    writer.Append(" ");
                    writer.Append(joinTable.TableAlias);

                    wheres.Add(joinTable.JoinExpression);
                }
            }

            wheres.AddRange(queryObject.Wheres);

            if (wheres.Count > 0)
            {
                writer.Append("\nWHERE ");
                for (var i = 0; i < wheres.Count; i++)
                {
                    var criteria = wheres[i];
                    if (i > 0)
                    {
                        writer.AppendLine(" AND");
                    }

                    WriteExpression(criteria, writer);
                }
            }
        }

        public override void WriteInsertQuery(List<InsertInfo> inserts, string fromTableName, string autoIncrementPrimaryKeyName, bool isLastStatement, StringBuilder writer)
        {
            // NOTE: Because variables are implemented with a per-query temporary table, every insert is wrapped to update
            // the "last inserted identity" variable (if the table has an autoincrementing primary key).
            if (!string.IsNullOrEmpty(autoIncrementPrimaryKeyName))
            {
                writer.AppendLine("WITH \"_typedsql_temp_identity\" AS (");
                WriteInsertBuilderQuery(inserts, fromTableName, writer);
                writer.Append(" RETURNING ");
                WriteColumnName(autoIncrementPrimaryKeyName, writer);
                writer.AppendLine(")");
                writer.Append("UPDATE \"_typedsql_variables\" SET \"_typedsql_last_insert_identity\" = \"_typedsql_temp_identity\".");
                WriteColumnName(autoIncrementPrimaryKeyName, writer);
                writer.Append(" FROM \"_typedsql_temp_identity\";");
            }
            else
            {
                WriteInsertBuilderQuery(inserts, fromTableName, writer);
            }
            writer.AppendLine(";");
        }

        public override void WriteInsertQuery(SqlQuery parentQueryResult, List<InsertInfo> inserts, string fromTableName, string autoIncrementPrimaryKeyName, bool isLastStatement, StringBuilder writer)
        {
            // NOTE: Because variables are implemented with a per-query temporary table, every insert is wrapped to update
            // the "last inserted identity" variable (if the table has an autoincrementing primary key).
            // This workaround loses the affected rows count after INSERT .. SELECT statements in "ExecuteNonQuery". 
            // However the affected row count is only returned back to the client for the last
            // statement in a batch. And in that case, the "last inserted identity" is certainly NOT going to be referenced
            // later in the SQL, so it is safe to ignore updating the "last inserted identity", and ensuring 
            // to return the true affected rows count if the insert is the last statement.

            if (!isLastStatement && !string.IsNullOrEmpty(autoIncrementPrimaryKeyName))
            {
                writer.AppendLine("WITH \"_typedsql_temp_identity\" AS (");
                WriteInsertBuilderQuery(parentQueryResult, inserts, fromTableName, writer);
                writer.Append(" RETURNING ");
                WriteColumnName(autoIncrementPrimaryKeyName, writer);
                writer.AppendLine(")");
                writer.Append("UPDATE \"_typedsql_variables\" SET \"_typedsql_last_insert_identity\" = (SELECT MAX(\"_typedsql_temp_identity\".");
                WriteColumnName(autoIncrementPrimaryKeyName, writer);
                writer.Append(") FROM \"_typedsql_temp_identity\") FROM \"_typedsql_temp_identity\";");
            }
            else
            {
                WriteInsertBuilderQuery(parentQueryResult, inserts, fromTableName, writer);
            }
            writer.AppendLine(";");
        }

        protected override void WriteAddForeignKeyOn(StringBuilder writer)
        {
            writer.Append("ON DELETE RESTRICT ");
            writer.AppendLine("ON UPDATE RESTRICT;");
        }

        protected override void WriteDropForeignKey(string fromTableName, string foreignKeyName, StringBuilder writer)
        {
            writer.Append("ALTER TABLE ");
            WriteTableName(fromTableName, writer);
            writer.Append(" DROP CONSTRAINT ");
            WriteColumnName(foreignKeyName, writer); // NOTE: not a column name
            writer.AppendLine(";");
        }

        protected override void WriteDropIndex(string fromTableName, string indexName, StringBuilder writer)
        {
            writer.Append("DROP INDEX ");
            WriteTableName(indexName, writer); // ?? not a table, but want quotes
            writer.AppendLine(";");
        }

        protected override void WriteFunctionCall(SqlCallExpression callExpr, StringBuilder writer)
        {
            if (callExpr.Method.Name == nameof(Function.Year))
            {
                writer.Append("EXTRACT(YEAR FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Month))
            {
                writer.Append("EXTRACT(MONTH FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Day))
            {
                writer.Append("EXTRACT(DAY FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else
            if (callExpr.Method.Name == nameof(Function.Hour))
            {
                writer.Append("EXTRACT(HOUR FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Minute))
            {
                writer.Append("EXTRACT(MINUTE FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Second))
            {
                writer.Append("EXTRACT(SECOND FROM ");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else
            {
                base.WriteFunctionCall(callExpr, writer);
            }
        }
    }
}
