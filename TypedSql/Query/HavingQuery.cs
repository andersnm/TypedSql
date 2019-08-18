using System;
using System.Linq.Expressions;

namespace TypedSql {

    public interface IHavingQuery {
        LambdaExpression HavingExpression { get; }
    }

    public class HavingQuery<TFrom, T> : AggregateQuery<TFrom, T>, IHavingQuery {

        public LambdaExpression HavingExpression { get; }

        public HavingQuery(Query<TFrom, T> parent, Expression<Func<T, bool>> expr) : base(parent) {
            HavingExpression = expr;
        }
    }
}
