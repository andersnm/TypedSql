using System;
using System.Collections.Generic;
using System.Text;
using TypedSql.Schema;

namespace TypedSql.MySql
{
    public class MySqlFormatter : SqlBaseFormatter
    {
        public override void WriteColumnName(string columnName, StringBuilder writer)
        {
            writer.Append("`");
            writer.Append(columnName);
            writer.Append("`");
        }

        public override void WriteTableName(string tableName, StringBuilder writer)
        {
            writer.Append("`");
            writer.Append(tableName);
            writer.Append("`");
        }

        public override string WriteColumnType(Type type, SqlTypeInfo sqlTypeInfo)
        {
            if (type == typeof(sbyte))
            {
                return "TINYINT";
            }
            else if (type == typeof(byte))
            {
                return "TINYINT UNSIGNED";
            }
            else if (type == typeof(short))
            {
                return "SMALLINT";
            }
            else if (type == typeof(ushort))
            {
                return "SMALLINT UNSIGNED";
            }
            else if (type == typeof(int))
            {
                return "INT";
            }
            else if (type == typeof(uint))
            {
                return "INT UNSIGNED";
            }
            else if (type == typeof(long))
            {
                return "BIGINT";
            }
            else if (type == typeof(ulong))
            {
                return "BIGINT UNSIGNED";
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
                return "REAL";
            }
            else if (type == typeof(string))
            {
                var length = sqlTypeInfo.StringLength > 0 ? sqlTypeInfo.StringLength.ToString() : "1024";
                return $"VARCHAR({length})";
            }
            else if (type == typeof(DateTime))
            {
                return "DATETIME";
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

        public override void WriteCreateTableColumn(SqlColumn column, StringBuilder writer)
        {
            WriteColumnName(column.Name, writer);
            writer.Append(" ");

            writer.Append(WriteColumnType(column.Type, column.SqlType));

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
                    writer.Append(" AUTO_INCREMENT");
                }
            }
        }

        public override void WriteDeclareSqlVariable(string name, Type type, SqlTypeInfo sqlTypeInfo, StringBuilder writer)
        {
            // NOTE: TODO: declare in sp, not in sesssion
            // throw new NotImplementedException();
        }

        public override void WriteSetSqlVariable(SqlPlaceholder variable, SqlExpression expr, StringBuilder writer)
        {
            writer.Append("SET ");
            WritePlaceholder(variable, writer);
            writer.Append(" = ");
            WriteExpression(expr, writer);
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
            writer.Append("LAST_INSERT_ID()");
        }

        public override void WriteIfNullExpression(SqlExpression testExpr, SqlExpression ifNullExpr, StringBuilder writer)
        {
            writer.Append("IFNULL(");
            WriteExpression(testExpr, writer);
            writer.Append(", ");
            WriteExpression(ifNullExpr, writer);
            writer.Append(")");
        }

        public override void WriteSelectQuery(SqlQuery queryObject, StringBuilder writer)
        {
            base.WriteSelectQuery(queryObject, writer);

            if (queryObject.Limit.HasValue)
            {
                writer.Append(" LIMIT ");
                writer.Append(queryObject.Limit.Value);

                if (queryObject.Offset.HasValue)
                {
                    writer.Append(" OFFSET ");
                    writer.Append(queryObject.Offset.Value);
                }
            }
        }

        public override void WriteUpdateQuery(List<InsertInfo> inserts, SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("UPDATE ");
            WriteFromSource(queryObject.From, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);

            foreach (var join in queryObject.Joins)
            {
                WriteJoin(join, writer);
            }

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

            WriteFinalQuery(queryObject, writer);
        }

        public override void WriteDeleteQuery(SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("DELETE ");
            writer.Append(queryObject.FromAlias);
            writer.Append(" FROM ");
            WriteFromSource(queryObject.From, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);
            writer.Append(" ");
            WriteJoins(queryObject, writer);
            WriteFinalQuery(queryObject, writer);
        }

        protected override void WriteAddForeignKeyOn(StringBuilder writer)
        {
            writer.Append("ON DELETE RESTRICT ");
            writer.AppendLine("ON UPDATE RESTRICT;");
        }

        protected override void WriteDropForeignKey(string fromTableName, string foreignKeyName, StringBuilder writer)
        {
            writer.AppendLine("ALTER TABLE " + fromTableName + " DROP FOREIGN KEY " + foreignKeyName + ";");
            /*
            writer.AppendLine("SELECT COUNT(*) INTO @FOREIGN_KEY_my_foreign_key_ON_TABLE_my_table_EXISTS");
            writer.AppendLine("FROM `information_schema`.`table_constraints`");
            writer.AppendLine("WHERE `table_schema` = 'typedsqltest'");
            writer.AppendLine("  AND `table_name` = '" + fromTableName + "'");
            writer.AppendLine("  AND `constraint_name` = '" + foreignKeyName + "'");
            writer.AppendLine("  AND `constraint_type` = 'FOREIGN KEY';");
            writer.AppendLine("SET @statement := IF(");
            writer.AppendLine("  @FOREIGN_KEY_my_foreign_key_ON_TABLE_my_table_EXISTS > 0,");
            writer.AppendLine("  'ALTER TABLE " + fromTableName + " DROP FOREIGN KEY " + foreignKeyName + "',");
            writer.AppendLine("  'SELECT 1');");
            writer.AppendLine("PREPARE statement FROM @statement;");
            writer.AppendLine("EXECUTE statement;");*/
        }
    }
}
