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
                Columns = Table.Columns,
            };
        }
    }

}
