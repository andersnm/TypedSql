using System;
using System.Collections.Generic;
using System.Linq;
using TypedSql.Schema;

namespace TypedSql
{
    public class SqlSchemaParser
    {
        public SqlCreateTable ParseCreateTable(IFromQuery table)
        {
            return new SqlCreateTable()
            {
                TableName = table.TableName,
                Columns = table.Columns.Select(c => new SqlColumn()
                {
                    Name = c.SqlName,
                    Type = c.BaseType,
                    Nullable = c.Nullable,
                    PrimaryKey = c.PrimaryKey,
                    PrimaryKeyAutoIncrement = c.PrimaryKeyAutoIncrement,
                    SqlType = c.SqlType,
                }).ToList(),
            };
        }

        public SqlDropTable ParseDropTable(IFromQuery table)
        {
            return new SqlDropTable()
            {
                TableName = table.TableName,
            };
        }

        public SqlAddForeignKey ParseAddForeignKey(IFromQuery table, ForeignKey foreignKey)
        {
            var foreignQuery = table.Context.FromQueries.Where(q => q.TableType == foreignKey.ReferenceTableType).FirstOrDefault();
            if (foreignQuery == null)
            {
                throw new InvalidOperationException("Foreign key referenced an invalid table type: " + foreignKey.ReferenceTableType.Name);
            }

            var columns = GetFieldNamesFromMemberNames(table, foreignKey.Columns);
            var referenceColumns = GetFieldNamesFromMemberNames(foreignQuery, foreignKey.ReferenceColumns);

            return new SqlAddForeignKey()
            {
                TableName = table.TableName,
                ForeignKey = new SqlForeignKey()
                {
                    Name = foreignKey.Name,
                    ReferenceTableName = foreignQuery.TableName,
                    Columns = columns,
                    ReferenceColumns = referenceColumns,
                },
            };
        }

        public SqlDropForeignKey ParseDropForeignKey(IFromQuery table, ForeignKey foreignKey)
        {
            return new SqlDropForeignKey()
            {
                TableName = table.TableName,
                ForeignKeyName = foreignKey.Name,
            };
        }

        public SqlAddIndex ParseAddIndex(IFromQuery table, Index index)
        {
            var columns = GetFieldNamesFromMemberNames(table, index.Columns);
            return new SqlAddIndex()
            {
                TableName = table.TableName,
                Index = new SqlIndex()
                {
                    Name = index.Name,
                    Unique = index.Unique,
                    Columns = columns,
                }
            };
        }

        public SqlDropIndex ParseDropIndex(IFromQuery table, Index index)
        {
            return new SqlDropIndex()
            {
                TableName = table.TableName,
                IndexName = index.Name,
            };
        }

        private List<string> GetFieldNamesFromMemberNames(IFromQuery fromQuery, List<string> members)
        {
            var columns = new List<string>();
            foreach (var keyColumn in members)
            {
                var fromColumns = fromQuery.Columns.Where(c => c.MemberName == keyColumn).First();
                columns.Add(fromColumns.SqlName);
            }

            return columns;
        }
    }
}
