using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TypedSql.InMemory;

namespace TypedSql
{
    public interface ISelectStatement : IStatement
    {
        // NOTE: one or two rgs! TODO: like project
        Query SelectQuery { get; }
        List<object> EvaluateInMemory(InMemoryQueryRunner runner);
    }

    public class SelectStatement<TFrom, T, TResult> : ISelectStatement
    {
        public Query SelectQuery { get; }
        internal Query<TFrom, TResult> SelectQueryTResult { get; }

        public SelectStatement(Query<TFrom, T> parent, Expression<Func<SelectorContext<T>, T, TResult>> selectExpression)
        {
            SelectQueryTResult = new ProjectQuery<TFrom, T, TResult>(parent, selectExpression);
            SelectQuery = SelectQueryTResult;
        }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            return SelectQueryTResult.InMemorySelect(runner).Cast<object>().ToList();
        }
    }

    public class SelectStatement<TResult> : ISelectStatement
    {
        public Query SelectQuery { get; }
        public Query<TResult, TResult> SelectQueryTResult { get; }

        public SelectStatement(Expression<Func<SelectorContext, TResult>> selectExpression)
        {
            SelectQueryTResult = new ProjectConstantQuery<TResult>(selectExpression);
            SelectQuery = SelectQueryTResult;
        }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            return SelectQueryTResult.InMemorySelect(runner).Cast<object>().ToList();
        }
    }
}
