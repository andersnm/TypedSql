using System.Linq;

namespace TypedSql
{
    public interface IDeleteStatement : IStatement
    {
        Query Parent { get; }
        int EvaluateInMemory(IQueryRunner runner);
    }

    public class DeleteStatement<T, TJoin> : IDeleteStatement where T: new()
    {
        public Query Parent { get; }
        private Query<T, TJoin> ParentTJoin { get; }
        private FromQuery<T> FromQueryT { get; }

        public DeleteStatement(FlatQuery<T, TJoin> parent)
        {
            Parent = parent;
            ParentTJoin = parent;
            FromQueryT = parent.GetFromQuery<T>();
        }

        public int EvaluateInMemory(IQueryRunner runner)
        {
            var items = ParentTJoin.InMemorySelect(runner).ToList();
            var lastNonQueryResult = 0;

            foreach (var item in items)
            {
                var fromRow = ParentTJoin.FromRowMapping[item];
                lastNonQueryResult += FromQueryT.DeleteObject(fromRow);
            }

            return lastNonQueryResult;
        }
    }
}
