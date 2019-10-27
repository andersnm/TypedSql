using System.Collections.Generic;
using System.Linq;

namespace TypedSql.CliTool
{
    public class SqlTableComparer
    {
        public static List<SqlStatement> CompareTables(List<SqlTable> previous, List<SqlTable> next)
        {
            var statements = new List<SqlStatement>();

            // Remove all FK's in previous
            foreach (var previousTable in previous)
            {
                foreach (var foreignKey in previousTable.ForeignKeys)
                {
                    statements.Add(new SqlDropForeignKey()
                    {
                        TableName = previousTable.TableName,
                        ForeignKeyName = foreignKey.Name,
                    });
                }
            }

            // Drop previous tables not present in next
            foreach (var previousTable in previous)
            {
                var nextTable = next.Where(n => n.TableName == previousTable.TableName).FirstOrDefault();
                if (nextTable == null)
                {
                    statements.Add(new SqlDropTable()
                    {
                        TableName = previousTable.TableName
                    });
                }
            }

            // Add new in next, or update modified
            foreach (var nextTable in next)
            {
                var previousTable = previous.Where(n => n.TableName == nextTable.TableName).FirstOrDefault();
                if (previousTable == null)
                {
                    // create new table
                    statements.Add(new SqlCreateTable()
                    {
                        TableName = nextTable.TableName,
                        Columns = nextTable.Columns,
                    });
                }
                else
                {
                    // Generate changes for columns and indices, but not foreign keys
                    CompareColumns(previousTable, nextTable, statements);
                    // CompareForeignKeys(previousTable, nextTable, statements);
                    CompareIndices(previousTable, nextTable, statements);
                }
            }

            // Insert all FK's in next
            foreach (var nextTable in next)
            {
                foreach (var foreignKey in nextTable.ForeignKeys)
                {
                    statements.Add(new SqlAddForeignKey()
                    {
                        TableName = nextTable.TableName,
                        ForeignKey = foreignKey,
                    });
                }
            }

            return statements;
        }

        static void CompareColumns(SqlTable previousTable, SqlTable nextTable, List<SqlStatement> statements)
        {
            foreach (var previousColumn in previousTable.Columns)
            {
                var nextColumn = nextTable.Columns.Where(c => c.Name == previousColumn.Name).FirstOrDefault();
                if (nextColumn == null)
                {
                    statements.Add(new SqlDropColumn()
                    {
                        TableName = nextTable.TableName,
                        ColumnName = previousColumn.Name,
                    });
                }
            }

            // Add new in next, or update modified
            foreach (var nextColumn in nextTable.Columns)
            {
                var previousColumn = previousTable.Columns.Where(c => c.Name == nextColumn.Name).FirstOrDefault();
                if (previousColumn == null)
                {
                    statements.Add(new SqlAddColumn()
                    {
                        TableName = nextTable.TableName,
                        Column = new SqlColumn() { 
                            Name = nextColumn.Name,
                            Type = nextColumn.Type,
                            SqlType = nextColumn.SqlType,
                        },
                    });
                }
                else
                {
                    if (CheckColumnChanged(previousColumn, nextColumn))
                    {
                        statements.Add(new SqlDropColumn()
                        {
                            TableName = nextTable.TableName,
                            ColumnName = previousColumn.Name,
                        });
                        statements.Add(new SqlAddColumn()
                        {
                            TableName = nextTable.TableName,
                            Column = nextColumn,
                        });
                    }
                }
            }
        }

        static void CompareForeignKeys(SqlTable previousTable, SqlTable nextTable, List<SqlStatement> statements)
        {
            // Find and remove dropped
            /*
             * The migration deletes all FKs before applying, so dont need to delete again
            foreach (var previousForeignKey in previousTable.ForeignKeys)
            {
                var nextForeignKey = nextTable.ForeignKeys.Where(c => c.Name == previousForeignKey.Name).FirstOrDefault();
                if (nextForeignKey == null)
                {
                    statements.Add(new SqlDropConstraint()
                    {
                        TableName = nextTable.TableName,
                        ForeignKeyName = previousForeignKey.Name,
                    });
                }
            }

            // Add new in next, or update modified
            foreach (var nextForeignKey in nextTable.ForeignKeys)
            {
                var previousForeignKey = previousTable.ForeignKeys.Where(c => c.Name == nextForeignKey.Name).FirstOrDefault();
                if (previousForeignKey == null)
                {
                    statements.Add(new SqlAddConstraint()
                    {
                        TableName = nextTable.TableName,
                        ForeignKey = nextForeignKey,
                    });
                }
                else
                {
                    if (CheckForeignKeyChanged(previousForeignKey, nextForeignKey))
                    {
                        statements.Add(new SqlModifyConstraint()
                        {
                            TableName = nextTable.TableName,
                            ForeignKeyName = nextForeignKey.Name,
                            Columns = nextForeignKey.Columns,
                            ReferenceColumns = nextForeignKey.ReferenceColumns,
                            ReferenceTableName = nextForeignKey.ReferenceTableName,
                        });
                    }
                }
            }
            */
        }

        static void CompareIndices(SqlTable previousTable, SqlTable nextTable, List<SqlStatement> statements)
        {
            // Find and remove dropped
            foreach (var previousIndex in previousTable.Indices)
            {
                var nextIndex = nextTable.Indices.Where(c => c.Name == previousIndex.Name).FirstOrDefault();
                if (nextIndex == null)
                {
                    statements.Add(new SqlDropForeignKey()
                    {
                        TableName = nextTable.TableName,
                        ForeignKeyName = previousIndex.Name,
                    });
                }
            }

            // Add new in next, or update modified
            foreach (var nextIndex in nextTable.Indices)
            {
                var previousIndex = previousTable.Indices.Where(c => c.Name == nextIndex.Name).FirstOrDefault();
                if (previousIndex == null)
                {
                    statements.Add(new SqlAddIndex()
                    {
                        TableName = nextTable.TableName,
                        Index = nextIndex,
                    });
                }
                else
                {
                    if (CheckIndexChanged(previousIndex, nextIndex))
                    {
                        statements.Add(new SqlDropIndex()
                        {
                            TableName = nextTable.TableName,
                            IndexName = previousIndex.Name,
                        });
                        statements.Add(new SqlAddIndex()
                        {
                            TableName = nextTable.TableName,
                            Index = nextIndex,
                        });
                    }
                }
            }
        }

        static bool CheckColumnChanged(SqlColumn lhs, SqlColumn rhs)
        {
            if (lhs.Type != rhs.Type)
            {
                return true;
            }

            if (lhs.PrimaryKey != rhs.PrimaryKey)
            {
                return true;
            }

            if (lhs.PrimaryKeyAutoIncrement != rhs.PrimaryKeyAutoIncrement)
            {
                return true;
            }

            if (lhs.Type == typeof(string))
            {
                if (lhs.SqlType.StringLength != rhs.SqlType.StringLength)
                {
                    return true;
                }

                if (lhs.SqlType.StringNVarChar != rhs.SqlType.StringNVarChar)
                {
                    return true;
                }
            }
            else if (lhs.Type == typeof(decimal) || lhs.Type == typeof(decimal?))
            {
                if (lhs.SqlType.DecimalPrecision != rhs.SqlType.DecimalPrecision)
                {
                    return true;
                }

                if (lhs.SqlType.DecimalScale != rhs.SqlType.DecimalScale)
                {
                    return true;
                }
            }

            return false;
        }

        static bool CheckForeignKeyChanged(SqlForeignKey lhs, SqlForeignKey rhs)
        {
            if (lhs.Columns.Count != lhs.Columns.Count)
            {
                return true;
            }

            for (var i = 0; i < lhs.Columns.Count; i++)
            {
                if (lhs.Columns[i] != rhs.Columns[i])
                {
                    return true;
                }
            }

            if (lhs.ReferenceTableName != rhs.ReferenceTableName)
            {
                return true;
            }

            if (lhs.ReferenceColumns.Count != lhs.ReferenceColumns.Count)
            {
                return true;
            }

            for (var i = 0; i < lhs.ReferenceColumns.Count; i++)
            {
                if (lhs.ReferenceColumns[i] != rhs.ReferenceColumns[i])
                {
                    return true;
                }
            }

            return false;
        }

        static bool CheckIndexChanged(SqlIndex lhs, SqlIndex rhs)
        {
            if (lhs.Columns.Count != lhs.Columns.Count)
            {
                return true;
            }

            for (var i = 0; i < lhs.Columns.Count; i++)
            {
                if (lhs.Columns[i] != rhs.Columns[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
