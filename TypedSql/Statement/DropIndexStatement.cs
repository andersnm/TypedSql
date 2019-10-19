namespace TypedSql
{
    public interface IDropIndexStatement : IStatement
    {
    }

    public class DropIndexStatement : IDropIndexStatement
    {
        public IFromQuery Table { get; }
        public Schema.Index Index { get; }

        public DropIndexStatement(IFromQuery table, Schema.Index index)
        {
            Table = table;
            Index = index;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlDropIndex()
            {
                TableName = Table.TableName,
                IndexName = Index.Name,
            };
        }
    }
}
