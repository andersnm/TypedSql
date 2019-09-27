namespace TypedSql
{
    public interface IDropTableStatement : IStatement
    {
    }

    public class DropTableStatement : IDropTableStatement
    {
        public IFromQuery Table { get; }

        public DropTableStatement(IFromQuery table)
        {
            Table = table;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlDropTable()
            {
                TableName = Table.TableName,
                // Columns = Table.Columns,
            };
        }
    }
}
