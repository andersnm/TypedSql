using System;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IUpdateStatement : IStatement
    {
        Query Parent { get; }
        IFromQuery FromQuery { get; }
        LambdaExpression InsertExpression { get; }

        int EvaluateInMemory(IQueryRunner runner);
    }

    public class UpdateStatement<T, TJoin> : IUpdateStatement where T: new()
    {
        public Query Parent { get; }
        public IFromQuery FromQuery { get; }
        public LambdaExpression InsertExpression { get; }

        private Query<T, TJoin> ParentTJoin { get; }
        private FromQuery<T> FromQueryT { get; }
        private Action<TJoin, InsertBuilder<T>> InsertFunction { get; }

        /// <summary>
        /// UPDATE ... SET with row parameter
        /// </summary>
        public UpdateStatement(FlatQuery<T, TJoin> query, Expression<Action<TJoin, InsertBuilder<T>>> insertExpr)
        {
            Parent = query;
            ParentTJoin = query;
            FromQuery = query.GetFromQuery<T>();
            FromQueryT = query.GetFromQuery<T>();
            InsertExpression = insertExpr;
            InsertFunction = insertExpr.Compile();
        }

        public int EvaluateInMemory(IQueryRunner runner)
        {
            var lastNonQueryResult = 0;

            var items = ParentTJoin.InMemorySelect(runner);

            foreach (var item in items)
            {
                var fromRow = ParentTJoin.FromRowMapping[item];
                var builder = new InsertBuilder<T>();
                InsertFunction(item, builder);

                FromQueryT.UpdateObject(fromRow, builder.BuilderType, builder);
                lastNonQueryResult++;
            }

            return lastNonQueryResult;
        }
    }
}
