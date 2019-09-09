using System;
using System.Collections.Generic;
using System.Reflection;

namespace TypedSql {
    public abstract class SqlMember
    {
        /// <summary>
        /// The name being used in C#.
        /// </summary>
        public string MemberName { get; set; }

        public PropertyInfo MemberInfo { get; set; }

        /// <summary>
        /// The type of this named ref
        /// </summary>
        public Type FieldType { get; set; }

        /// <summary>
        /// The name being referenced in SQL.
        /// </summary>
        public string SqlName { get; set; }
    }

    public class SqlTableFieldMember : SqlMember
    {
        public string TableAlias { get; set; }
        public Type TableType { get; set; }
    }

    public class SqlJoinFieldMember : SqlMember
    {
        public string JoinAlias { get; set; }
        public SqlMember SourceField { get; set; }
    }

    public class SqlExpressionMember : SqlMember
    {
        public SqlExpression Expression { get; set; }
    }

    public abstract class SqlJoin
    {
        public SqlExpression JoinExpression { get; set; }
        public SqlSubQueryResult JoinResult { get; set; }
        public JoinType JoinType { get; set; }
    }

    public class SqlJoinTable : SqlJoin
    {
        public string TableAlias { get; set; }
        public SqlFrom FromSource { get; set; }
    }

    public class SqlJoinSubQuery : SqlJoin
    {
        public string JoinAlias { get; set; }
        public SqlQuery JoinFrom { get; set; }
    }

    public abstract class SqlFrom
    {
    }

    public class SqlFromTable : SqlFrom
    {
        public string TableName { get; set; }
    }

    public class SqlFromSubQuery : SqlFrom
    {
        public SqlQuery FromQuery { get; set; }
    }

    public class SqlOrderBy
    {
        public bool Ascending { get;set; }
        public SqlExpression SelectorExpression { get; set; }
    }

    public class SqlQuery
    {
        public List<SqlExpression> Wheres { get; set; } = new List<SqlExpression>();
        public List<SqlExpression> GroupBys { get; set; } = new List<SqlExpression>();
        public List<SqlExpression> Havings { get; set; } = new List<SqlExpression>();
        public List<SqlJoin> Joins { get; set; } = new List<SqlJoin>();
        public List<SqlOrderBy> OrderBys { get; set; } = new List<SqlOrderBy>();

        public string FromAlias { get; set; }
        public SqlFrom From { get; set; }

        public SqlSubQueryResult SelectResult { get; set; }

        public int? Offset { get; set; }
        public int? Limit { get; set; }
    }

    public class SqlSubQueryResult
    {
        public List<SqlMember> Members { get; set; }
    }
}
