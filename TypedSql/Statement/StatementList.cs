using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IStatement
    {
        SqlStatement Parse(SqlQueryParser parser);
    }

    public class StatementResult<TResult>
    {
        public StatementList StatementList { get; set; }
        public ISelectStatement Statement { get; set; }
    }

    public class StatementList
    {
        public List<IStatement> Queries { get; }
        public StatementList Scope { get; }
        public StatementList RootScope => Scope != null ? Scope.RootScope : this;

        public StatementList()
        {
            Queries = new List<IStatement>();
        }

        public StatementList(StatementList scope)
        {
            Queries = new List<IStatement>();
            Scope = scope;
        }

        public void Add(IStatement query)
        {
            Queries.Add(query);
        }

        /// <summary>
        /// SELECT ... FROM ...
        /// </summary>
        public StatementResult<T> Select<TFrom, T>(Query<TFrom, T> query)
        {
            var stmt = new SelectStatement<TFrom, T>(query);
            Queries.Add(stmt);
            return new StatementResult<T>()
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
        public void Update<T, TJoin>(FlatQuery<T, TJoin> query, Expression<Action<TJoin, InsertBuilder<T>>> insertExpr) where T : new()
        {
            Queries.Add(new UpdateStatement<T, TJoin>(query, insertExpr));
        }

        public void Delete<T, TJoin>(FlatQuery<T, TJoin> query) where T : new()
        {
            Queries.Add(new DeleteStatement<T, TJoin>(query));
        }

        /// <summary>
        /// INSERT INTO (...) VALUES ( ...)
        /// </summary>
        public void Insert<T>(FromQuery<T> query, Expression<Action<InsertBuilder<T>>> insertExpr) where T : new()
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

        public SqlPlaceholder<T> DeclareSqlVariable<T>(string name)
        {
            Queries.Add(new DeclareVariableStatement<T>(name));

            return new SqlPlaceholder<T>()
            {
                RawSql = name,
                PlaceholderType = SqlPlaceholderType.SessionVariableName,
            };
        }

        public void SetSqlVariable<T>(SqlPlaceholder<T> placeholder, Expression<Func<SelectorContext<T>, T>> node)
        {
            Queries.Add(new SetVariableStatement<T>(placeholder, node));
        }
    }
}
