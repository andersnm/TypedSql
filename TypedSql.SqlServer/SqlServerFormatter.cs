using System;
using System.Collections.Generic;
using System.Text;

namespace TypedSql.SqlServer
{
    public class SqlServerFormatter : CommonFormatter
    {
        public override void WriteColumnName(string columnName, StringBuilder writer) {
            writer.Append("[");
            writer.Append(columnName);
            writer.Append("]");
        }

        public override void WriteTableName(string tableName, StringBuilder writer)
        {
            writer.Append("[");
            writer.Append(tableName);
            writer.Append("]");
        }

        public override void WriteCreateTableColumn(Schema.Column column, StringBuilder writer)
        {
            WriteColumnName(column.SqlName, writer);
            writer.Append(" ");

            writer.Append(WriteColumnType(column.BaseType));

            if (column.Nullable)
            {
                writer.Append(" NULL");
            }
            else
            {
                writer.Append(" NOT NULL");
            }

            if (column.PrimaryKey)
            {
                writer.Append(" PRIMARY KEY");

                if (column.PrimaryKeyAutoIncrement)
                {
                    writer.Append(" IDENTITY(1, 1)");
                }
            }
        }

        public override string WriteColumnType(Type type)
        {
            if (type == typeof(sbyte))
            {
                throw new InvalidOperationException("Signed byte is not supported in SQL Server");
            }
            else if (type == typeof(byte))
            {
                return "TINYINT";
            }
            else if (type == typeof(short))
            {
                return "SMALLINT";
            }
            else if (type == typeof(ushort))
            {
                throw new InvalidOperationException("Unsigned short is not supported in SQL Server");
            }
            else if (type == typeof(int))
            {
                return "INT";
            }
            else if (type == typeof(uint))
            {
                throw new InvalidOperationException("Unsigned int is not supported in SQL Server");
            }
            else if (type == typeof(long))
            {
                return "BIGINT";
            }
            else if (type == typeof(ulong))
            {
                throw new InvalidOperationException("Unsigned long is not supported in SQL Server");
            }
            else if (type == typeof(decimal))
            {
                return "DECIMAL(13, 5)";
            }
            else if (type == typeof(float))
            {
                return "REAL";
            }
            else if (type == typeof(double))
            {
                return "REAL";
            }
            else if (type == typeof(string))
            {
                return "NVARCHAR(MAX)";
            }
            else if (type == typeof(DateTime))
            {
                return "DATETIME2";
            }
            else if (type == typeof(bool))
            {
                return "BIT";
            }
            else
            {
                throw new InvalidOperationException("FormatType unsupported type " + type.ToString());
            }
        }

        public override void WriteSetSqlVariable(SqlPlaceholder variable, SqlExpression expr, StringBuilder writer)
        {
            writer.Append("SET ");
            WritePlaceholder(variable, writer);
            writer.Append(" = (");
            WriteExpression(expr, writer);
            writer.AppendLine(");");
        }

        public override void WriteDeclareSqlVariable(string name, Type type, StringBuilder writer)
        {
            writer.Append("DECLARE @");
            writer.Append(name);
            writer.Append(" ");
            writer.Append(WriteColumnType(type));
            writer.AppendLine(";");
        }

        public override void WritePlaceholder(SqlPlaceholder placeholder, StringBuilder writer)
        {
            if (placeholder.PlaceholderType == SqlPlaceholderType.SessionVariableName)
            {
                writer.Append("@" + placeholder.RawSql);
            }
            else if (placeholder.PlaceholderType == SqlPlaceholderType.RawSqlExpression)
            {
                writer.Append(placeholder.RawSql);
            }
            else
            {
                throw new InvalidOperationException("Unsupported placeholder " + placeholder.PlaceholderType);
            }
        }

        public override void WriteLastIdentityExpression(StringBuilder writer)
        {
            writer.Append("SELECT CAST(SCOPE_IDENTITY() AS INT)");
        }

        public override void WriteIfNullExpression(SqlExpression testExpr, SqlExpression ifNullExpr, StringBuilder writer)
        {
            writer.Append("ISNULL(");
            WriteExpression(testExpr, writer);
            writer.Append(", ");
            WriteExpression(ifNullExpr, writer);
            writer.Append(")");
        }

        public override void WriteSelectQuery(SqlQuery queryObject, StringBuilder writer)
        {
            base.WriteSelectQuery(queryObject, writer);

            // SQL 2012+ https://stackoverflow.com/a/9261762
            if (queryObject.Offset.HasValue)
            {
                writer.AppendLine();
                writer.Append(" OFFSET ");
                writer.Append(queryObject.Offset.Value);
                writer.Append(" ROWS");
            }

            if (queryObject.Limit.HasValue)
            {
                writer.AppendLine();
                writer.Append(" FETCH NEXT ");
                writer.Append(queryObject.Limit.Value);
                writer.Append(" ROWS ONLY");
            }
        }

        public override void WriteUpdateQuery(List<InsertInfo> inserts, SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("UPDATE ");
            writer.Append(queryObject.FromAlias);

            writer.Append(" SET ");

            for (var i = 0; i < inserts.Count; i++)
            {
                var insert = inserts[i];

                if (i > 0)
                {
                    writer.Append(", ");
                }

                writer.Append(queryObject.FromAlias);
                writer.Append(".");
                WriteColumnName(insert.SqlName, writer);
                writer.Append(" = ");
                WriteExpression(insert.Expression, writer);
            }

            if (!(queryObject.From is SqlFromTable tableFromSource))
            {
                throw new InvalidOperationException("Must update on table");
            }

            writer.Append(" FROM ");
            WriteTableName(tableFromSource.TableName, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);
            WriteJoins(queryObject, writer);
            WriteFinalQuery(queryObject, writer);
        }

        public override void WriteDeleteQuery(SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("DELETE ");
            writer.Append(queryObject.FromAlias);
            writer.Append(" FROM ");

            if (!(queryObject.From is SqlFromTable tableFromSource))
            {
                throw new InvalidOperationException("Must delete from table");
            }

            WriteTableName(tableFromSource.TableName, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);
            writer.Append(" ");

            WriteJoins(queryObject, writer);
            WriteFinalQuery(queryObject, writer);
        }
    }
}
