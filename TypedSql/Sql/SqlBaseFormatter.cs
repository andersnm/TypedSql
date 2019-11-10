using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TypedSql.Migration;
using TypedSql.Schema;

namespace TypedSql
{
    public abstract class SqlBaseFormatter
    {
        public abstract string WriteColumnType(Type type, SqlTypeInfo sqlTypeInfo);
        public abstract void WriteCreateTableColumn(SqlColumn column, StringBuilder writer);
        public abstract void WriteDeclareSqlVariable(string name, Type type, SqlTypeInfo sqlTypeInfo, StringBuilder writer);
        public abstract void WriteDeleteQuery(SqlQuery queryObject, StringBuilder writer);
        public abstract void WritePlaceholder(SqlPlaceholder placeholder, StringBuilder writer);
        public abstract void WriteSetSqlVariable(SqlPlaceholder variable, SqlExpression expr, StringBuilder writer);
        public abstract void WriteTableName(string tableName, StringBuilder writer);
        public abstract void WriteColumnName(string tableName, StringBuilder writer);
        public abstract void WriteUpdateQuery(List<InsertInfo> inserts, SqlQuery queryObject, StringBuilder writer);
        public abstract void WriteLastIdentityExpression(StringBuilder writer);
        public abstract void WriteIfNullExpression(SqlExpression testExpr, SqlExpression ifNullExpr, StringBuilder writer);

        public void WriteStatement(SqlStatement stmt, bool isLastStatement, StringBuilder sb)
        {
            switch (stmt)
            {
                case SqlInsert insert:
                    WriteInsertQuery(insert.Inserts, insert.TableName, insert.AutoIncrementPrimaryKeyName, isLastStatement, sb);
                    return;
                case SqlInsertSelect insertSelect:
                    WriteInsertQuery(insertSelect.FromSource, insertSelect.Inserts, insertSelect.TableName, insertSelect.AutoIncrementPrimaryKeyName, isLastStatement, sb);
                    return;
                case SqlSelect select:
                    WriteSelectQuery(select.FromSource, sb);
                    sb.AppendLine(";");
                    return;
                case SqlUpdate update:
                    WriteUpdateQuery(update.Inserts, update.FromSource, sb);
                    return;
                case SqlDelete delete:
                    WriteDeleteQuery(delete.FromSource, sb);
                    return;
                case SqlDeclareVariable declareVariable:
                    WriteDeclareSqlVariable(declareVariable.VariableName, declareVariable.VariableType, declareVariable.SqlTypeInfo, sb);
                    return;
                case SqlSet set:
                    WriteSetSqlVariable(set.Variable, set.Expression, sb);
                    return;
                case SqlCreateTable createTable:
                    WriteCreateTable(createTable.TableName, createTable.Columns, sb);
                    return;
                case SqlDropTable dropTable:
                    WriteDropTable(dropTable.TableName, true, sb);
                    return;
                case SqlAddColumn addColumn:
                    WriteAddColumn(addColumn.TableName, addColumn.Column, sb);
                    return;
                case SqlDropColumn dropColumn:
                    WriteDropColumn(dropColumn.TableName, dropColumn.ColumnName, sb);
                    return;
                case SqlAddForeignKey addForeignKey:
                    WriteAddForeignKey(addForeignKey.TableName, addForeignKey.ForeignKey, sb);
                    return;
                case SqlDropForeignKey dropForeignKey:
                    WriteDropForeignKey(dropForeignKey.TableName, dropForeignKey.ForeignKeyName, sb);
                    return;
                case SqlAddIndex addIndex:
                    WriteAddIndex(addIndex.TableName, addIndex.Index, sb);
                    return;
                case SqlDropIndex dropIndex:
                    WriteDropIndex(dropIndex.TableName, dropIndex.IndexName, sb);
                    return;
            }

            throw new Exception("Unsupported statement " + stmt.GetType().Name);
        }

        void WriteCreateTable(string fromTableName, List<SqlColumn> columns, StringBuilder writer)
        {
            writer.Append("CREATE TABLE ");
            WriteTableName(fromTableName, writer);
            writer.Append(" (");

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteCreateTableColumn(column, writer);
            }

            writer.Append(")");
            writer.AppendLine(";");
        }

        void WriteDropTable(string fromTableName, bool useIfExists, StringBuilder writer)
        {
            // TODO: override for sql server <2016
            writer.Append("DROP TABLE ");
            if (useIfExists)
            {
                writer.Append("IF EXISTS ");
            }

            WriteTableName(fromTableName, writer);
            writer.AppendLine(";");
        }

        protected virtual void WriteAddColumn(string fromTableName, SqlColumn column, StringBuilder writer)
        {
            writer.Append("ALTER TABLE ");
            WriteTableName(fromTableName, writer);
            writer.Append(" ADD ");
            WriteCreateTableColumn(column, writer);
            writer.AppendLine(";");
        }

        protected virtual void WriteDropColumn(string fromTableName, string columnName, StringBuilder writer)
        {
            writer.Append("ALTER TABLE ");
            WriteTableName(fromTableName, writer);
            writer.Append(" DROP COLUMN ");
            WriteColumnName(columnName, writer);
            writer.AppendLine(";");
        }


        protected virtual void WriteAddForeignKey(string fromTableName, SqlForeignKey foreignKey, StringBuilder writer)
        {
            WriteAddForeignKeyReference(fromTableName, foreignKey, writer);
            WriteAddForeignKeyOn(writer);
        }

        protected virtual void WriteAddForeignKeyReference(string fromTableName, SqlForeignKey foreignKey, StringBuilder writer)
        {
            writer.Append("ALTER TABLE ");
            WriteTableName(fromTableName, writer);
            writer.Append(" ADD CONSTRAINT ");
            WriteColumnName(foreignKey.Name, writer); // NOTE: not a column name
            writer.Append(" FOREIGN KEY (");

            WriteColumnNames(foreignKey.Columns, writer);
            writer.Append(") REFERENCES ");

            WriteTableName(foreignKey.ReferenceTableName, writer);
            writer.Append(" (");

            WriteColumnNames(foreignKey.ReferenceColumns, writer);
            writer.Append(") ");
        }

        protected void WriteColumnNames(List<string> columnNames, StringBuilder writer)
        {
            for (var i = 0; i < columnNames.Count; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteColumnName(columnNames[i], writer);
            }
        }

        protected abstract void WriteAddForeignKeyOn(StringBuilder writer);

        protected abstract void WriteDropForeignKey(string fromTableName, string foreignKeyName, StringBuilder writer);

        void WriteAddIndex(string fromTableName, SqlIndex index, StringBuilder writer)
        {
            writer.Append("CREATE ");
            if (index.Unique)
            {
                writer.Append("UNIQUE ");
            }
            writer.Append("INDEX ");
            WriteTableName(index.Name, writer); // NOTE: not a column name
            writer.Append(" ON ");
            WriteTableName(fromTableName, writer);
            writer.Append(" (");
            WriteColumnNames(index.Columns, writer);
            writer.AppendLine(");");
        }

        protected virtual void WriteDropIndex(string fromTableName, string indexName, StringBuilder writer)
        {
            writer.Append("DROP INDEX ");
            WriteTableName(indexName, writer); // ?? not a table, but want quotes
            writer.Append(" ON ");
            WriteTableName(fromTableName, writer);
            writer.AppendLine(";");
        }

        public virtual void WriteExpression(SqlExpression node, StringBuilder writer)
        {
            if (node is SqlBinaryExpression binary)
            {
                var leftType = binary.Left.GetExpressionType();
                var rightType = binary.Right.GetExpressionType();

                // TODO: check if null being registered as parameter constant
                if (binary.Right is SqlConstantExpression constExpr && constExpr.Value == null)
                {
                    WriteExpression(binary.Left, writer);
                    if (binary.Op == ExpressionType.Equal)
                    {
                        writer.Append(" IS NULL");
                    }
                    else if (binary.Op == ExpressionType.NotEqual)
                    {
                        writer.Append(" IS NOT NULL");
                    }
                    else
                    {
                        throw new Exception(binary.Op.ToString());
                    }
                }
                else if (leftType == typeof(string) && rightType == typeof(string) && binary.Op == ExpressionType.Add)
                {
                    writer.Append("CONCAT(");
                    WriteExpression(binary.Left, writer);
                    writer.Append(", ");
                    WriteExpression(binary.Right, writer);
                    writer.Append(")");
                }
                else if (leftType == typeof(string) && rightType == typeof(string) && binary.Op == ExpressionType.Quote)
                {
                    // like haxx using Quote op
                    WriteExpression(binary.Left, writer);
                    writer.Append(" LIKE ");
                    WriteExpression(binary.Right, writer);
                }
                else if (binary.Op == ExpressionType.Coalesce)
                {
                    WriteIfNullExpression(binary.Left, binary.Right, writer);
                }
                else
                {
                    writer.Append("(");
                    WriteExpression(binary.Left, writer);
                    if (binary.Op == ExpressionType.Equal)
                    {
                        writer.Append("=");
                    }
                    else if (binary.Op == ExpressionType.NotEqual)
                    {
                        writer.Append("<>");
                    }
                    else if (binary.Op == ExpressionType.GreaterThan)
                    {
                        writer.Append(">");
                    }
                    else if (binary.Op == ExpressionType.GreaterThanOrEqual)
                    {
                        writer.Append(">=");
                    }
                    else if (binary.Op == ExpressionType.LessThan)
                    {
                        writer.Append("<");
                    }
                    else if (binary.Op == ExpressionType.LessThanOrEqual)
                    {
                        writer.Append("<=");
                    }
                    else if (binary.Op == ExpressionType.Add)
                    {
                        writer.Append("+");
                    }
                    else if (binary.Op == ExpressionType.Subtract)
                    {
                        writer.Append("-");
                    }
                    else if (binary.Op == ExpressionType.Multiply)
                    {
                        writer.Append("*");
                    }
                    else if (binary.Op == ExpressionType.Divide)
                    {
                        writer.Append("/");
                    }
                    else if (binary.Op == ExpressionType.AndAlso)
                    {
                        writer.Append(" AND ");
                    }
                    else if (binary.Op == ExpressionType.OrElse)
                    {
                        writer.Append(" OR ");
                    }
                    else
                    {
                        throw new Exception("Unhandled binary operation " + binary.Op.ToString());
                    }
                    WriteExpression(binary.Right, writer);
                    writer.Append(")");
                }
            }
            else if (node is SqlCastExpression castExpr)
            {
                if (castExpr.TargetType.IsConstructedGenericType && castExpr.TargetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // Unwrap nullable cast
                    WriteExpression(castExpr.Operand, writer);
                }
                else if (IsNumericType(castExpr.TargetType) && IsNumericType(castExpr.Operand.GetExpressionType()))
                {
                    // Unwrap numeric<->numeric and enum<->numeric casts
                    WriteExpression(castExpr.Operand, writer);
                }
                else
                {
                    throw new NotImplementedException("Cast to " + castExpr.TargetType.Name + " not implemented");
                }
            }
            else if (node is SqlConstantExpression constant)
            {
                if (constant.Value == null)
                {
                    writer.Append("NULL");
                }
                else
                {
                    writer.Append(constant.Value);
                }
            }
            else if (node is SqlTableFieldExpression tableField)
            {
                writer.Append(tableField.TableFieldRef.TableAlias);
                writer.Append(".");
                WriteColumnName(tableField.TableFieldRef.SqlName, writer);
            }
            else if (node is SqlJoinFieldExpression joinField)
            {
                writer.Append(joinField.JoinFieldRef.JoinAlias);
                writer.Append(".");
                WriteColumnName(joinField.JoinFieldRef.SqlName, writer);
            }
            else if (node is SqlPlaceholderExpression placeholder)
            {
                WritePlaceholder(placeholder.Placeholder, writer);
            }
            else if (node is SqlNegateExpression negate)
            {
                writer.Append("-");
                WriteExpression(negate.Operand, writer);
            }
            else if (node is SqlNotExpression notExpr)
            {
                writer.Append("NOT ");
                WriteExpression(notExpr.Operand, writer);
            }
            else if (node is SqlCallExpression callExpr)
            {
                WriteFunctionCall(callExpr, writer);
            }
            else if (node is SqlConditionalExpression condExpr)
            {
                // Special handling for:
                // - "x != null ? x.Value : null" in SQL where x is a table/result type
                // - "x.Value != null ? (T)x.Value : 0" in SQL where x.Value is a Nullable<T> type
                if (IsConditionalNullTableCastToNullable(condExpr, out var nullCastExpr))
                {
                    WriteExpression(nullCastExpr, writer);
                }
                else if (IsIfNull(condExpr, out var ifNullTestExpr, out var ifNullExpr))
                {
                    WriteIfNullExpression(ifNullTestExpr, ifNullExpr, writer);
                }
                else
                {
                    // TODO: CASE WHEN ELSE END
                    throw new Exception("Expected BinaryExpression in Conditional in a known form."); // the form 'x != null ? x.Value : null'");
                }
            }
            else if (node is SqlSelectExpression selectExpr)
            {
                writer.Append("(");
                WriteSelectQuery(selectExpr.Query, writer);
                writer.Append(")");
            }
            else
            {
                throw new Exception("Unhandled SQL expression " + node.GetType().Name);
            }
        }

        protected virtual void WriteFunctionCall(SqlCallExpression callExpr, StringBuilder writer)
        {
            if (callExpr.Method.Name == nameof(Function.Contains))
            {
                WriteExpression(callExpr.Arguments[0], writer);
                writer.Append(" IN (");
                if (callExpr.Arguments[1] is SqlConstantArrayExpression array)
                {
                    writer.Append(string.Join(", ", array.Value));
                }
                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Count))
            {
                writer.Append("COUNT(*)"); // TODO: count selector expr, override for *
            }
            else if (callExpr.Method.Name == nameof(Function.Sum))
            {
                writer.Append("SUM(");

                var selector = (SqlSelectorExpression)callExpr.Arguments[1];
                WriteExpression(selector.SelectorExpression, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Average))
            {
                writer.Append("AVG(");

                var selector = (SqlSelectorExpression)callExpr.Arguments[1];
                WriteExpression(selector.SelectorExpression, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Min))
            {
                writer.Append("MIN(");

                var selector = (SqlSelectorExpression)callExpr.Arguments[1];
                WriteExpression(selector.SelectorExpression, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Max))
            {
                writer.Append("MAX(");

                var selector = (SqlSelectorExpression)callExpr.Arguments[1];
                WriteExpression(selector.SelectorExpression, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.LastInsertIdentity))
            {
                WriteLastIdentityExpression(writer);
            }
            else if (callExpr.Method.Name == nameof(Function.Year))
            {
                writer.Append("YEAR(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Month))
            {
                writer.Append("MONTH(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Day))
            {
                writer.Append("DAY(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Hour))
            {
                writer.Append("HOUR(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Minute))
            {
                writer.Append("MINUTE(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else if (callExpr.Method.Name == nameof(Function.Second))
            {
                writer.Append("SECOND(");

                var dateExpr = callExpr.Arguments[0];
                WriteExpression(dateExpr, writer);

                writer.Append(")");
            }
            else
            {
                throw new NotImplementedException("Function." + callExpr.Method.Name);
            }
        }

        private bool IsIfNull(SqlConditionalExpression condExpr, out SqlExpression testExpression, out SqlExpression ifNullExpression)
        {
            // Detect conditional IFNULL() expressions
            // "x.Value != null ? (T)x.Value : 0" in SQL where x.Value is a Nullable < T > type
            testExpression = null;
            ifNullExpression = null;

            if (!(condExpr.Test is SqlBinaryExpression condTestExpr))
            {
                return false;
            }

            if (!IsConditionalNullableField(condTestExpr.Left, out var condTestLeftExpr))
            {
                return false;
            }

            if (condTestExpr.Op != ExpressionType.NotEqual)
            {
                return false;
            }

            if (!(condTestExpr.Right is SqlConstantExpression condTestRightExpr && condTestRightExpr.Value == null))
            {
                return false;
            }

            if (!(condExpr.IfTrue is SqlCastExpression condTrueCastExpr))
            {
                return false;
            }

            if (!IsConditionalNullableField(condTrueCastExpr.Operand, out var condTrueCastOperandExpr))
            {
                return false;
            }

            if (condTestLeftExpr != condTrueCastOperandExpr)
            {
                return false;
            }

            testExpression = condTestLeftExpr;
            ifNullExpression = condExpr.IfFalse;
            return true;
        }

        protected bool IsNumericType(Type type) {
            return type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
                || type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// A nullable member expression like 'a.Value' is represented
        /// as a conditional 'a != null ? a.Value : null'.
        /// This method detects nullable expressions, and returns only 
        /// the field expression.
        /// </summary>
        bool IsConditionalNullableField(SqlExpression operand, out SqlExpression outputExpression)
        {
            if (!(operand is SqlConditionalExpression condTrueCastCondExpr))
            {
                outputExpression = null;
                return false;
            }

            if (!IsConditionalNullTableCastToNullable(condTrueCastCondExpr, out var condTrueCastCondTrueExpr))
            {
                outputExpression = null;
                return false;
            }

            // TODO: check if return cast target is non-nullable
            if (!(condTrueCastCondTrueExpr is SqlCastExpression condTrueCastCondTrueCastExpr))
            {
                outputExpression = null;
                return false;
            }

            outputExpression = condTrueCastCondTrueCastExpr.Operand;
            return true;
        }

        private bool IsConditionalNullTableCastToNullable(SqlConditionalExpression condExpr, out SqlExpression outputExpression) {
            // Detect conditional casts to nullables in C# which translates directly to f.ex just the field name in SQL
            // "x != null ? x.Value : null" where x is a table/result type
            if (!(condExpr.Test is SqlBinaryExpression testExpr))
            {
                outputExpression = null;
                return false;
            }

            if (testExpr.Left is SqlTableExpression && testExpr.Right is SqlConstantExpression rightExpr && rightExpr.Value == null && testExpr.Op == ExpressionType.NotEqual)
            {
                outputExpression = condExpr.IfTrue;
                return true;
            }

            // "x == null ? null : x.Value" where x is a table/result type
            if (testExpr.Right is SqlTableExpression && testExpr.Left is SqlConstantExpression leftExpr && leftExpr.Value == null && testExpr.Op == ExpressionType.Equal)
            {
                outputExpression = condExpr.IfFalse;
                return true;
            }

            outputExpression = null;
            return false;
        }

        public virtual void WriteQueryObject(SqlMember member, StringBuilder writer)
        {
            if (member is SqlTableFieldMember tableFieldRef)
            {
                writer.Append(tableFieldRef.TableAlias);
                writer.Append(".");
                WriteColumnName(member.SqlName, writer);
            }
            else if (member is SqlJoinFieldMember joinFieldRef)
            {
                writer.Append(joinFieldRef.JoinAlias);
                writer.Append(".");
                WriteColumnName(member.SqlName, writer);
            }
            else if (member is SqlExpressionMember exprRef)
            {
                WriteExpression(exprRef.Expression, writer);
            }
            else
            {
                throw new NotImplementedException(member.GetType().Name);
            }
        }

        void GetFlatSelectMembers(List<SqlMember> members, string prefix, List<Tuple<string, SqlMember>> result)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                prefix += "_";
            }

            foreach (var member in members)
            {
                if (member is SqlExpressionMember exprMember && exprMember.Expression is SqlTableExpression tableExpression)
                {
                    GetFlatSelectMembers(tableExpression.TableResult.Members, prefix + exprMember.MemberName, result);
                }
                else
                {
                    result.Add(new Tuple<string, SqlMember>(prefix, member));
                }
            }
        }

        public virtual void WriteSelectQuery(SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append("SELECT ");

            var flatMembers = new List<Tuple<string, SqlMember>>();
            GetFlatSelectMembers(queryObject.SelectResult.Members, "", flatMembers);

            for (var i = 0; i < flatMembers.Count; i++)
            {
                if (i > 0)
                {
                    writer.Append(", ");
                }

                var queryMember = flatMembers[i];

                WriteQueryObject(queryMember.Item2, writer);
                writer.Append(" AS ");
                WriteColumnName(queryMember.Item1 + queryMember.Item2.MemberName, writer);
            }

            if (queryObject.From != null)
            {
                WriteFromQuery(queryObject, writer);
            }

            // writer.AppendLine(";");
        }

        public virtual void WriteInsertQuery(List<InsertInfo> inserts, string fromTableName, string autoIncrementPrimaryKeyName, bool isLastStatement, StringBuilder writer)
        {
            // autoIncrementPrimaryKeyName, isLastStatement are unused here, but used by the PostgreSQL override
            WriteInsertBuilderQuery(inserts, fromTableName, writer);
            writer.AppendLine(";");
        }

        protected virtual void WriteInsertBuilderQuery(List<InsertInfo> inserts, string fromTableName, StringBuilder writer)
        {
            WriteInsertBuilderPrefix(inserts, fromTableName, writer);
            writer.Append("VALUES (");

            for (var i = 0; i < inserts.Count; i++)
            {
                var insert = inserts[i];

                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteExpression(insert.Expression, writer);
            }

            writer.Append(")");
        }

        public virtual void WriteInsertQuery(SqlQuery parentQueryResult, List<InsertInfo> inserts, string fromTableName, string autoIncrementPrimaryKeyName, bool isLastStatement, StringBuilder writer)
        {
            // autoIncrementPrimaryKeyName, isLastStatement are unused here, but used by the PostgreSQL override
            WriteInsertBuilderQuery(parentQueryResult, inserts, fromTableName, writer);
            writer.AppendLine(";");
        }

        protected virtual void WriteInsertBuilderQuery(SqlQuery parentQueryResult, List<InsertInfo> inserts, string fromTableName, StringBuilder writer)
        {
            WriteInsertBuilderPrefix(inserts, fromTableName, writer);
            writer.Append("SELECT ");

            for (var i = 0; i < inserts.Count; i++)
            {
                var insert = inserts[i];

                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteExpression(insert.Expression, writer);
            }

            WriteFromQuery(parentQueryResult, writer);
        }

        protected virtual void WriteInsertBuilderPrefix(List<InsertInfo> inserts, string fromTableName, StringBuilder writer)
        {
            writer.Append("INSERT INTO ");
            WriteTableName(fromTableName, writer);
            writer.Append(" (");

            for (var i = 0; i < inserts.Count; i++)
            {
                var insert = inserts[i];
                if (i > 0)
                {
                    writer.Append(", ");
                }

                WriteColumnName(insert.SqlName, writer);
            }

            writer.Append(") ");
        }

        protected virtual void WriteFromQuery(SqlQuery queryObject, StringBuilder writer)
        {
            writer.Append(" FROM ");
            WriteFromSource(queryObject.From, writer);
            writer.Append(" ");
            writer.Append(queryObject.FromAlias);
            WriteJoins(queryObject, writer);
            WriteFinalQuery(queryObject, writer);
        }

        protected virtual void WriteJoin(SqlJoin join, StringBuilder writer)
        {
            if (join.JoinType == JoinType.InnerJoin)
            {
                writer.Append("\nINNER JOIN ");
            }
            else if (join.JoinType == JoinType.LeftJoin)
            {
                writer.Append("\nLEFT JOIN ");
            }
            else
            {
                throw new NotImplementedException(join.JoinType.ToString());
            }

            if (join is SqlJoinTable joinTable)
            {
                WriteFromSource(joinTable.FromSource, writer);
                writer.Append(" ");
                writer.Append(joinTable.TableAlias);
                writer.Append(" ON ");
                WriteExpression(join.JoinExpression, writer);
            }
            else if (join is SqlJoinSubQuery joinSubquery)
            {
                writer.Append(" (");
                WriteSelectQuery(joinSubquery.JoinFrom, writer);
                writer.Append(") ");
                writer.Append(joinSubquery.JoinAlias);
                writer.Append(" ON ");
                WriteExpression(join.JoinExpression, writer);
            }
            else
            {
                throw new NotImplementedException(join.GetType().Name);
            }
        }

        protected void WriteJoins(SqlQuery queryObject, StringBuilder writer)
        {
            foreach (var join in queryObject.Joins)
            {
                WriteJoin(join, writer);
            }
        }

        protected void WriteFinalQuery(SqlQuery queryObject, StringBuilder writer)
        {
            if (queryObject.Wheres.Count > 0)
            {
                writer.Append("\nWHERE ");
                for (var i = 0; i < queryObject.Wheres.Count; i++)
                {
                    var criteria = queryObject.Wheres[i];
                    if (i > 0)
                    {
                        writer.Append(" AND\n");
                    }

                    WriteExpression(criteria, writer);
                }
            }

            if (queryObject.GroupBys.Count > 0)
            {
                writer.Append("\nGROUP BY ");
                for (var i = 0; i < queryObject.GroupBys.Count; i++)
                {
                    var grouping = queryObject.GroupBys[i];
                    if (i > 0)
                    {
                        writer.Append(", ");
                    }

                    WriteExpression(grouping, writer);
                }
            }

            if (queryObject.Havings.Count > 0)
            {
                writer.Append("\nHAVING ");
                for (var i = 0; i < queryObject.Havings.Count; i++)
                {
                    var having = queryObject.Havings[i];
                    if (i > 0)
                    {
                        writer.Append(" AND\n");
                    }

                    WriteExpression(having, writer);
                }
            }

            if (queryObject.OrderBys.Count > 0)
            {
                writer.Append("\nORDER BY ");
                for (var i = 0; i < queryObject.OrderBys.Count; i++)
                {
                    var orderBy = queryObject.OrderBys[i];
                    if (i > 0)
                    {
                        writer.Append(", ");
                    }

                    WriteExpression(orderBy.SelectorExpression, writer);

                    if (!orderBy.Ascending)
                    {
                        writer.Append(" DESC");
                    }
                }
            }
        }

        protected virtual void WriteFromSource(SqlFrom fromSource, StringBuilder writer)
        {
            if (fromSource is SqlFromTable tableFromSource)
            {
                WriteTableName(tableFromSource.TableName, writer);
            }
            else if (fromSource is SqlFromSubQuery subQueryFromSource)
            {
                writer.AppendLine("(");
                WriteSelectQuery(subQueryFromSource.FromQuery, writer);
                writer.Append(")");
            }
        }
    }
}
