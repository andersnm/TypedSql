using System;
using System.Collections.Generic;
using System.Text;

namespace TypedSql
{
    public interface IFormatter
    {
        void WriteSelectQuery(SqlQuery queryObject, StringBuilder writer);
        void WriteInsertBuilderQuery(List<InsertInfo> inserts, IFromQuery query, StringBuilder writer);
        void WriteInsertBuilderQuery(SqlQuery parentQueryResult, List<InsertInfo> inserts, IFromQuery query, StringBuilder writer);
        void WriteUpdateQuery(List<InsertInfo> inserts, SqlQuery queryObject, StringBuilder writer);
        void WriteDeleteQuery(SqlQuery queryObject, StringBuilder writer);
        void WriteDeclareSqlVariable(string name, Type type, StringBuilder writer);
        void WriteSetSqlVariable(SqlPlaceholder variable, SqlExpression expr, StringBuilder writer);

        // TODO: replace these with create table/drop table statements
        void WriteTableName(string tableName, StringBuilder writer);
        void WriteCreateTableColumn(Schema.Column column, StringBuilder writer);
    }
}
