using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TypedSql
{
    public static class QueryRunnerExtensions
    {
        /// <summary>
        /// SELECT ... FROM ...
        /// </summary>
        public static IEnumerable<T> Select<TFrom, T>(this IQueryRunner runner, Query<TFrom, T> query)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(query);
            return runner.ExecuteQuery(select);
        }

        /// <summary>
        /// SELECT ... (without FROM)
        /// </summary>
        public static IEnumerable<T> Select<T>(this IQueryRunner runner, Expression<Func<SelectorContext, T>> selectExpr)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(selectExpr);
            return runner.ExecuteQuery(select);
        }

        /// <summary>
        /// UPDATE tbl JOIN tbl2 SET tbl.X = tbl2.Y ...
        /// </summary>
        public static int Update<T, TJoin>(this IQueryRunner runner, FlatQuery<T, TJoin> query, Expression<Action<TJoin, InsertBuilder<T>>> insertExpr) where T : new()
        {
            var stmtList = new StatementList();
            stmtList.Update(query, insertExpr);
            return runner.ExecuteNonQuery(stmtList);
        }

        public static int Delete<T, TJoin>(this IQueryRunner runner, FlatQuery<T, TJoin> query) where T : new()
        {
            var stmtList = new StatementList();
            stmtList.Delete(query);
            return runner.ExecuteNonQuery(stmtList);
        }

        /// <summary>
        /// INSERT INTO (...) VALUES ( ...)
        /// </summary>
        public static int Insert<T>(this IQueryRunner runner, FromQuery<T> query, Expression<Action<InsertBuilder<T>>> insertExpr) where T : new()
        {
            var stmtList = new StatementList();
            stmtList.Insert(query, insertExpr);
            return runner.ExecuteNonQuery(stmtList);
        }

        /// <summary>
        /// INSERT INTO (...) VALUES ( ...); SELECT LAST_IDENTITY()
        /// </summary>
        public static TIdentity Insert<T, TIdentity>(this IQueryRunner runner, FromQuery<T> query, Expression<Action<InsertBuilder<T>>> insertExpr) where T : new()
        {
            var stmtList = new StatementList();
            stmtList.Insert(query, insertExpr);
            var select = stmtList.Select(ctx => Function.LastInsertIdentity<TIdentity>(ctx));
            return runner.ExecuteQuery(select).FirstOrDefault();
        }

        /// <summary>
        /// INSERT INTO (...) SELECT ...
        /// </summary>
        public static int Insert<T, TSubFrom, TSub>(this IQueryRunner runner, FromQuery<T> query, Query<TSubFrom, TSub> subQuery, Expression<Action<TSub, InsertBuilder<T>>> insertExpr) where T : new()
        {
            var stmtList = new StatementList();
            stmtList.Insert(query, subQuery, insertExpr);
            return runner.ExecuteNonQuery(stmtList);
        }
    }
}
