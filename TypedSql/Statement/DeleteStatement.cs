using System.Linq;

namespace TypedSql
{
    public interface IDeleteStatement : IStatement
    {
        int EvaluateInMemory(IQueryRunner runner);
    }

    public class DeleteStatement<T, TJoin> : IDeleteStatement where T: new()
    {
        private Query<T, TJoin> Parent { get; }
        private FromQuery<T> FromQuery { get; }

        public DeleteStatement(FlatQuery<T, TJoin> parent)
        {
            Parent = parent;
            FromQuery = parent.GetFromQuery<T>();
        }

        public int EvaluateInMemory(IQueryRunner runner)
        {
            var items = Parent.InMemorySelect(runner).ToList();
            var lastNonQueryResult = 0;

            foreach (var item in items)
            {
                var fromRow = Parent.FromRowMapping[item];
                lastNonQueryResult += FromQuery.DeleteObject(fromRow);
            }

            return lastNonQueryResult;
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            return new SqlDelete()
            {
                FromSource = parser.ParseQuery(Parent),
            };
        }
    }
}
