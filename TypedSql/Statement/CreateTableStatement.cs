namespace TypedSql
{
    public interface ICreateTableStatement : IStatement
    {
        IFromQuery Table { get; }
    }

    public class CreateTableStatement : ICreateTableStatement
    {
        public IFromQuery Table { get; }

        public CreateTableStatement(IFromQuery table)
        {
            Table = table;
        }
    }
}
