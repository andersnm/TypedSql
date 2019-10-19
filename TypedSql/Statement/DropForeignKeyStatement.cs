namespace TypedSql
{
    public interface IDropForeignKeyStatement : IStatement
    {
    }

    public class DropForeignKeyStatement : IDropForeignKeyStatement
    {
        public IFromQuery Table { get; }
        public Schema.ForeignKey ForeignKey { get; }

        public DropForeignKeyStatement(IFromQuery table, Schema.ForeignKey foreignKey)
        {
            Table = table;
            ForeignKey = foreignKey;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlDropForeignKey()
            {
                TableName = Table.TableName,
                ForeignKeyName = ForeignKey.Name,
            };
        }
    }
}
