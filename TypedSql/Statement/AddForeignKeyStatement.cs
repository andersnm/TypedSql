using System;
using System.Collections.Generic;
using System.Linq;

namespace TypedSql
{
    public interface IAddForeignKeyStatement : IStatement
    {
    }

    public class AddForeignKeyStatement : IAddForeignKeyStatement
    {
        public IFromQuery Table { get; }
        public Schema.ForeignKey ForeignKey { get; }

        public AddForeignKeyStatement(IFromQuery table, Schema.ForeignKey foreignKey)
        {
            Table = table;
            ForeignKey = foreignKey;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var foreignQuery = Table.Context.FromQueries.Where(q => q.TableType == ForeignKey.ReferenceTableType).FirstOrDefault();
            if (foreignQuery == null)
            {
                throw new InvalidOperationException("Foreign key referenced an invalid table type: " + ForeignKey.ReferenceTableType.Name);
            }

            var columns = GetFieldNamesFromMemberNames(Table, ForeignKey.Columns);
            var referenceColumns = GetFieldNamesFromMemberNames(foreignQuery, ForeignKey.ReferenceColumns);

            return new SqlAddForeignKey()
            {
                TableName = Table.TableName,
                ForeignKey = new SqlForeignKey()
                {
                    Name = ForeignKey.Name,
                    ReferenceTableName = foreignQuery.TableName,
                    Columns = columns,
                    ReferenceColumns = referenceColumns,
                },
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
