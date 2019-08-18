﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IStatement
    {
    }

    public class StatementResult<TResult>
    {
        public SqlStatementList StatementList { get; set; }
        public ISelectStatement Statement { get; set; }
    }

    public class SqlStatementList
    {
        public List<IStatement> Queries { get; set; }

        public SqlStatementList()
        {
            Queries = new List<IStatement>();
        }

        public void Add(IStatement query)
        {
            Queries.Add(query);
        }

        /// <summary>
        /// SELECT ... FROM ...
        /// </summary>
        public StatementResult<TResult> Select<TFrom, T, TResult>(Query<TFrom, T> query, Expression<Func<SelectorContext<T>, T, TResult>> selectExpr)
        {
            var stmt = new SelectStatement<TFrom, T, TResult>(query, selectExpr);
            Queries.Add(stmt);
            return new StatementResult<TResult>()
            {
                Statement = stmt,
                StatementList = this
            };
        }

        /// <summary>
        /// SELECT ... (without FROM)
        /// </summary>
        public StatementResult<TResult> Select<TResult>(Expression<Func<SelectorContext, TResult>> selectExpr)
        {
            var stmt = new SelectStatement<TResult>(selectExpr);
            Queries.Add(stmt);
            return new StatementResult<TResult>()
            {
                Statement = stmt,
                StatementList = this
            };
        }

        /// <summary>
        /// UPDATE tbl JOIN tbl2 SET tbl.X = tbl2.Y ...
        /// </summary>
        public void Update<T, TJoin>(FlatQuery<T, TJoin> query, Expression<Action<TJoin, InsertBuilder<T>>> insertExpr) where T: new()
        {
            Queries.Add(new UpdateStatement<T, TJoin>(query, insertExpr));
        }

        public void Delete<T, TJoin>(FlatQuery<T, TJoin> query) where T: new()
        {
            Queries.Add(new DeleteStatement<T, TJoin>(query));
        }

        /// <summary>
        /// INSERT INTO (...) VALUES ( ...)
        /// </summary>
        public void Insert<T>(FromQuery<T> query, Expression<Action<InsertBuilder<T>>> insertExpr) where T: new()
        {
            Queries.Add(new InsertStatement<T>(query, insertExpr));
        }

        /// <summary>
        /// INSERT INTO (...) SELECT ...
        /// </summary>
        public void Insert<T, STFrom, ST>(FromQuery<T> query, Query<STFrom, ST> subQuery, Expression<Action<ST, InsertBuilder<T>>> insertExpr) where T : new()
        {
            Queries.Add(new InsertSelectStatement<T, STFrom, ST>(query, subQuery, insertExpr));
        }

        public SqlPlaceholder<T> DeclareSqlVariable<T>(string name) where T : IComparable, IConvertible
        {
            Queries.Add(new DeclareVariableStatement<T>(name));

            return new SqlPlaceholder<T>()
            {
                RawSql = name,
                PlaceholderType = SqlPlaceholderType.SessionVariableName,
            };
        }

        public void SetSqlVariable<T>(SqlPlaceholder<T> placeholder, Expression<Func<SelectorContext<T>, T>> node) where T : IComparable, IConvertible
        {
            Queries.Add(new SetVariableStatement<T>(placeholder, node));
        }
    }
}