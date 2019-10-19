using System.Linq;

namespace TypedSql
{
    public interface ICreateTableStatement : IStatement
    {
    }

    public class CreateTableStatement : ICreateTableStatement
    {
        public IFromQuery Table { get; }

        public CreateTableStatement(IFromQuery table)
        {
            Table = table;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlCreateTable()
            {
                TableName = Table.TableName,
                Columns = Table.Columns.Select(c => new SqlColumn()
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
    }
}
