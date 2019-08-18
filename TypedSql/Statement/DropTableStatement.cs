namespace TypedSql
{
    public interface IDropTableStatement : IStatement
    {
        IFromQuery Table { get; }
    }

    public class DropTableStatement : IDropTableStatement
    {
        public IFromQuery Table { get; }

        public DropTableStatement(IFromQuery table)
        {
            Table = table;
        }
    }
}
