using System.Collections.Generic;
using System.Linq;

namespace TypedSql
{
    public interface IDeleteStatement : IStatement
    {
        int EvaluateInMemory(IQueryRunner runner);
    }

    public class DeleteStatement<T, TJoin> : IDeleteStatement
        where T : new()
    {
        public DeleteStatement(FlatQuery<T, TJoin> parent)
        {
            Parent = parent;
            FromQuery = parent.GetFromQuery<T>();
        }

        private Query<T, TJoin> Parent { get; }
        private FromQuery<T> FromQuery { get; }

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
                FromSource = Parent.Parse(parser, new Dictionary<string, SqlSubQueryResult>()),
            };
        }
    }
}
