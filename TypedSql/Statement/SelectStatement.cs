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

    public class SelectStatement<TFrom, T> : ISelectStatement
    {
        public Query SelectQuery { get; }
        internal Query<TFrom, T> SelectQueryT { get; }

        public SelectStatement(Query<TFrom, T> parent)
        {
            SelectQueryT = parent;
            SelectQuery = SelectQueryT;
        }

        public List<object> EvaluateInMemory(InMemoryQueryRunner runner)
        {
            return SelectQueryT.InMemorySelect(runner).Cast<object>().ToList();
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
