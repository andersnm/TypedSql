using System.Collections.Generic;
using System.Linq;

namespace TypedSql
{
    public interface IAddIndexStatement : IStatement
    {
    }

    public class AddIndexStatement : IAddIndexStatement
    {
        public IFromQuery Table { get; }
        public Schema.Index Index { get; }

        public AddIndexStatement(IFromQuery table, Schema.Index index)
        {
            Table = table;
            Index = index;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var columns = GetFieldNamesFromMemberNames(Table, Index.Columns);
            return new SqlAddIndex()
            {
                TableName = Table.TableName,
                Index = new SqlIndex()
                {
                    Name = Index.Name,
                    Columns = columns,
                }
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
