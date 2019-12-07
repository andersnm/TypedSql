using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TypedSql
{
    public class InsertInfo
    {
        public SqlExpression Expression { get; set; }
        public string SqlName { get; set; }
    }

    public class SqlAliasProvider
    {
        private int counter = 0;

        public string CreateAlias()
        {
            var alias = "t" + counter.ToString();
            counter++;
            return alias;
        }
    }

    public class SqlQueryParser
    {
        public Dictionary<string, object> Constants { get; } = new Dictionary<string, object>();
        public SqlAliasProvider AliasProvider { get; } = new SqlAliasProvider();

        public List<SqlStatement> ParseStatementList(StatementList stmtList)
        {
            var result = new List<SqlStatement>();
            foreach (var stmt in stmtList.Queries)
            {
                result.Add(stmt.Parse(this));
            }

            return result;
        }

        public SqlExpression ParseExpression(LambdaExpression node)
        {
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            return ParseExpression(node.Body, parameters);
        }

        private SqlSelectorExpression ParseAggregateSelector(LambdaExpression lambdaExpr, Dictionary<string, SqlSubQueryResult> parameters, SqlSubQueryResult selectorSource)
        {
            var xprm = new Dictionary<string, SqlSubQueryResult>(parameters);
            xprm[lambdaExpr.Parameters[0].Name] = selectorSource;

            return new SqlSelectorExpression()
            {
                SelectorExpression = ParseExpression(lambdaExpr.Body, xprm),
            };
        }

        private bool IsQueryType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return false;
            }

            if (type.GetGenericTypeDefinition() == typeof(Query<,>))
            {
                return true;
            }

            if (typeInfo.BaseType != null)
            {
                return IsQueryType(typeInfo.BaseType);
            }

            return false;
        }

        private SqlExpression ParseFunctionCallExpression(MethodCallExpression call, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (IsQueryType(call.Method.DeclaringType))
            {
                if (call.Method.Name == nameof(Query<int, int>.AsExpression))
                {
                    var queryLambda = Expression.Lambda(call.Object);
                    var query = (Query)queryLambda.Compile().DynamicInvoke();
                    var sqlQuery = query.Parse(this);
                    return new SqlSelectExpression()
                    {
                        Query = sqlQuery
                    };
                }
                else
                {
                    throw new Exception("Unsupported query method call " + call.Method.Name);
                }
            }
            else if (call.Object == null && call.Method.DeclaringType == typeof(Function))
            {
                if (call.Method.Name == nameof(Function.Count))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseAggregateSelector((LambdaExpression)call.Arguments[1], parameters, ((SqlTableExpression)arg0).TableResult);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.Sum))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseAggregateSelector((LambdaExpression)call.Arguments[1], parameters, ((SqlTableExpression)arg0).TableResult);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.Average))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseAggregateSelector((LambdaExpression)call.Arguments[1], parameters, ((SqlTableExpression)arg0).TableResult);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.Min))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseAggregateSelector((LambdaExpression)call.Arguments[1], parameters, ((SqlTableExpression)arg0).TableResult);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.Max))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseAggregateSelector((LambdaExpression)call.Arguments[1], parameters, ((SqlTableExpression)arg0).TableResult);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.Contains))
                {
                    // TODO: one of:
                    // - Contains(ctx, value, query)
                    // - Contains(value, list)
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseExpression(call.Arguments[1], parameters);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0, arg1 }
                    };
                }
                else if (call.Method.Name == nameof(Function.LastInsertIdentity))
                {
                    // var arg0 = ParseExpression(call.Arguments[0], parameters);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { }
                    };
                }
                else if (call.Method.Name == nameof(Function.Like))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);
                    var arg1 = ParseExpression(call.Arguments[1], parameters);

                    return new SqlBinaryExpression()
                    {
                        Op = SqlBinaryOperator.Like,
                        Left = arg0,
                        Right = arg1,
                    };
                }
                else if (
                    call.Method.Name == nameof(Function.Year) ||
                    call.Method.Name == nameof(Function.Month) ||
                    call.Method.Name == nameof(Function.Day) ||
                    call.Method.Name == nameof(Function.Hour) ||
                    call.Method.Name == nameof(Function.Minute) ||
                    call.Method.Name == nameof(Function.Second))
                {
                    var arg0 = ParseExpression(call.Arguments[0], parameters);

                    return new SqlCallExpression()
                    {
                        Method = call.Method,
                        Arguments = new List<SqlExpression> { arg0 }
                    };
                }
                else
                {
                    throw new InvalidOperationException("Unsupported Function." + call.Method.Name);
                }
            }
            else
            {
                throw new Exception("Unsupported method call " + call.Method.Name);
            }
        }

        public SqlExpression ParseExpression(Expression node, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                var call = (MethodCallExpression)node;
                return ParseFunctionCallExpression(call, parameters);
            }
            else if (node.NodeType == ExpressionType.Parameter)
            {
                var parameterExpression = (ParameterExpression)node;
                return new SqlTableExpression
                {
                    TableResult = parameters[parameterExpression.Name]
                };
            }
            else if (node.NodeType == ExpressionType.MemberAccess)
            {
                var member = (MemberExpression)node;

                var thisAsMember = TryParseSqlMember(member.Expression, member.Member.Name, parameters);
                if (thisAsMember != null)
                {
                    return GetExpressionForSqlMember(thisAsMember);
                }

                var memberThisType = member.Member.DeclaringType;
                if (memberThisType.IsConstructedGenericType &&
                    memberThisType.GetGenericTypeDefinition() == typeof(SqlPlaceholder<>) &&
                    member.Member.Name == nameof(SqlPlaceholder<bool>.Value))
                {
                    var placeholder = (SqlPlaceholder)ResolveConstant(member.Expression, out var placeholderType);
                    return new SqlPlaceholderExpression()
                    {
                        Placeholder = placeholder,
                    };
                }

                // Handle as C# constant:
                // E.g lookups on this, local parameter variables, complex C# variables
                object value = ResolveConstant(member, out var memberType);
                return GetConstantExpression(value, memberType);
            }
            else if (node is ConstantExpression constant)
            {
                return GetConstantExpression(constant.Value, constant.Type);
            }
            else if (node is BinaryExpression binary)
            {
                return new SqlBinaryExpression()
                {
                    Left = ParseExpression(binary.Left, parameters),
                    Right = ParseExpression(binary.Right, parameters),
                    Op = GetSqlBinaryOperator(node.NodeType),
                };
            }
            else if (node is UnaryExpression convert && node.NodeType == ExpressionType.Convert)
            {
                return new SqlCastExpression()
                {
                    Operand = ParseExpression(convert.Operand, parameters),
                    TargetType = convert.Type,
                };
            }
            else if (node is UnaryExpression negate && node.NodeType == ExpressionType.Negate)
            {
                return new SqlNegateExpression()
                {
                    Operand = ParseExpression(negate.Operand, parameters),
                };
            }
            else if (node is UnaryExpression notExpr && node.NodeType == ExpressionType.Not)
            {
                return new SqlNotExpression()
                {
                    Operand = ParseExpression(notExpr.Operand, parameters),
                };
            }
            else if (node is ConditionalExpression condExpr)
            {
                return new SqlConditionalExpression()
                {
                    Test = ParseExpression(condExpr.Test, parameters),
                    IfTrue = ParseExpression(condExpr.IfTrue, parameters),
                    IfFalse = ParseExpression(condExpr.IfFalse, parameters),
                };
            }
            else if (node is NewExpression newExpression)
            {
                var members = new List<SqlMember>();
                for (var i = 0; i < newExpression.Arguments.Count; i++)
                {
                    var argumentExpression = newExpression.Arguments[i];
                    var member = newExpression.Members[i];

                    var exprResult = ParseExpression(argumentExpression, parameters);
                    ParseSelectNewExpression(exprResult, member, members);
                }

                return new SqlTableExpression()
                {
                    TableResult = new SqlSubQueryResult()
                    {
                        Members = members,
                    }
                };
            }
            else if (node is MemberInitExpression initExpression)
            {
                var members = new List<SqlMember>();
                for (var i = 0; i < initExpression.Bindings.Count; i++)
                {
                    var binding = initExpression.Bindings[i];
                    var member = binding.Member;

                    if (binding.BindingType == MemberBindingType.Assignment)
                    {
                        var assignment = (MemberAssignment)binding;
                        var exprResult = ParseExpression(assignment.Expression, parameters);
                        ParseSelectNewExpression(exprResult, member, members);
                    }
                    else
                    {
                        throw new NotImplementedException(binding.BindingType.ToString());
                    }
                }

                return new SqlTableExpression()
                {
                    TableResult = new SqlSubQueryResult()
                    {
                        Members = members,
                    }
                };
            }
            else
            {
                throw new Exception("Unsupported expression " + node.NodeType);
            }
        }

        private SqlBinaryOperator GetSqlBinaryOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return SqlBinaryOperator.Equal;
                case ExpressionType.NotEqual:
                    return SqlBinaryOperator.NotEqual;
                case ExpressionType.GreaterThan:
                    return SqlBinaryOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return SqlBinaryOperator.GreaterThanOrEqual;
                case ExpressionType.LessThan:
                    return SqlBinaryOperator.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return SqlBinaryOperator.LessThanOrEqual;
                case ExpressionType.Add:
                    return SqlBinaryOperator.Add;
                case ExpressionType.Subtract:
                    return SqlBinaryOperator.Subtract;
                case ExpressionType.Multiply:
                    return SqlBinaryOperator.Multiply;
                case ExpressionType.Divide:
                    return SqlBinaryOperator.Divide;
                case ExpressionType.Modulo:
                    return SqlBinaryOperator.Modulo;
                case ExpressionType.AndAlso:
                    return SqlBinaryOperator.AndAlso;
                case ExpressionType.OrElse:
                    return SqlBinaryOperator.OrElse;
                case ExpressionType.Coalesce:
                    return SqlBinaryOperator.Coalesce;
            }

            throw new InvalidOperationException("Operator not supported " + type.ToString());
        }

        private SqlMember TryParseSqlMember(Expression expression, string memberName, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (expression is ParameterExpression parameterExpression)
            {
                var subQuery = parameters[parameterExpression.Name];
                return subQuery.Members.Where(m => m.MemberName == memberName).First();
            }
            else
            if (expression is MemberExpression memberExpression)
            {
                var thisAsMember = TryParseSqlMember(memberExpression.Expression, memberExpression.Member.Name, parameters);
                if (thisAsMember is SqlExpressionMember exprRef && exprRef.Expression is SqlTableExpression tableExpression)
                {
                    return tableExpression.TableResult.Members.Where(m => m.MemberName == memberName).First();
                }
            }

            return null;
        }

        private SqlExpression GetConstantExpression(object value, Type type)
        {
            // byte array = blob
            if (!(value is byte[]) && value is IEnumerable enumerable && !(value is string))
            {
                return new SqlConstantArrayExpression()
                {
                    Value = enumerable.Cast<object>().ToList(),
                };
            }

            if (value == null)
            {
                return new SqlConstantExpression()
                {
                    ConstantType = type
                };
            }

            if (type.GetTypeInfo().IsEnum)
            {
                // Avoid Npgsqls enum handling by casting enums to int
                value = Convert.ChangeType(value, typeof(int));
            }

            var key = RegisterConstant(value);
            return new SqlConstantExpression()
            {
                Value = "@" + key,
                ConstantType = type,
            };
        }

        private object ResolveConstant(Expression expr, out Type constantType)
        {
            if (expr is MemberExpression memberExpr)
            {
                if (memberExpr.Expression == null)
                {
                    // Static class member, f.ex DateTime.Now
                    return GetObjectMember(null, memberExpr.Member, out constantType);
                }
                else
                {
                    var thisValue = ResolveConstant(memberExpr.Expression, out var thisType);
                    return GetObjectMember(thisValue, memberExpr.Member, out constantType);
                }
            }
            else if (expr is ConstantExpression constant)
            {
                constantType = constant.Type;
                return constant.Value;
            }
            else
            {
                throw new Exception("Unhandled constant expression: " + expr.NodeType);
            }
        }

        private object GetObjectMember(object self, MemberInfo member, out Type type)
        {
            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
            {
                type = fieldInfo.FieldType;
                return fieldInfo.GetValue(self);
            }

            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
            {
                type = propertyInfo.PropertyType;
                return propertyInfo.GetValue(self);
            }
            else
            {
                throw new InvalidOperationException("Unexpected MemberInfo " + member.GetType().Name);
            }
        }

        private SqlExpression GetExpressionForSqlMember(SqlMember subQueryMember)
        {
            if (subQueryMember is SqlTableFieldMember tableFieldRef)
            {
                return new SqlTableFieldExpression
                {
                    TableFieldRef = tableFieldRef,
                };
            }
            else if (subQueryMember is SqlJoinFieldMember joinFieldRef)
            {
                return new SqlJoinFieldExpression
                {
                    JoinFieldRef = joinFieldRef,
                };
            }
            else if (subQueryMember is SqlExpressionMember exprRef)
            {
                return exprRef.Expression;
            }
            else
            {
                throw new Exception("Expected table or join field parameter expression");
            }
        }

        public List<SqlMember> ParseSelectExpression(LambdaExpression selectExpression, Dictionary<string, SqlSubQueryResult> parameters)
        {
            var expr = ParseExpression(selectExpression.Body, parameters);
            if (expr is SqlTableExpression tableExpression)
            {
                // Select all table members
                return tableExpression.TableResult.Members;
            }
            else if (expr is SqlTableFieldExpression fieldExpression)
            {
                // Select scalar field
                return new List<SqlMember>()
                {
                    fieldExpression.TableFieldRef,
                };
            }
            else
            {
                // Select scalar expression result
                return new List<SqlMember>()
                {
                    new SqlExpressionMember()
                    {
                        Expression = expr,
                        FieldType = expr.GetExpressionType(),
                        MemberName = "Value",
                        SqlName = "Value",
                    }
                };
            }
        }

        private void ParseSelectNewExpression(SqlExpression exprResult, MemberInfo member, List<SqlMember> members)
        {
            members.Add(new SqlExpressionMember()
            {
                Expression = exprResult,
                MemberName = member.Name,
                MemberInfo = (PropertyInfo)member,
                SqlName = member.Name,
                FieldType = exprResult.GetExpressionType(),
            });
        }

        public List<InsertInfo> ParseInsertBuilder<T>(IFromQuery fromQuery, LambdaExpression insertExpr, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (insertExpr.Body.NodeType != ExpressionType.Call)
            {
                throw new InvalidOperationException("Insert expression can only call InsertBuilder<T>.Value()");
            }

            var callExpression = (MethodCallExpression)insertExpr.Body;
            var values = new List<InsertInfo>();
            while (true)
            {
                if (callExpression.Method.DeclaringType.GetGenericTypeDefinition() != typeof(InsertBuilder<>))
                {
                    throw new InvalidOperationException("Expected InsertBuilder<T> in insert expression");
                }

                if (callExpression.Method.Name == nameof(InsertBuilder<bool>.Values))
                {
                    var builder = (InsertBuilder<T>)ResolveConstant(callExpression.Arguments[0], out var argumentType);
                    foreach (var selector in builder.Selectors)
                    {
                        var constExpr = GetConstantExpression(selector.Value, selector.Value.GetType());
                        var insertInfo = ParseInsertBuilderValue<T>(fromQuery, selector.Selector, constExpr);
                        values.Add(insertInfo);
                    }
                }
                else if (callExpression.Method.Name == nameof(InsertBuilder<bool>.Value))
                {
                    var fieldSelectorUnary = (UnaryExpression)callExpression.Arguments[0];
                    var fieldSelector = (LambdaExpression)fieldSelectorUnary.Operand;
                    var valueExpression = callExpression.Arguments[1];
                    var memberValueExpression = ParseExpression(valueExpression, parameters);
                    var insertInfo = ParseInsertBuilderValue<T>(fromQuery, fieldSelector, memberValueExpression);
                    values.Add(insertInfo);
                }
                else
                {
                    throw new InvalidOperationException("Insert expression can only call InsertBuilder<T>.Value() or .Values()");
                }

                if (callExpression.Object.NodeType == ExpressionType.Parameter)
                {
                    break;
                }

                callExpression = (MethodCallExpression)callExpression.Object;
            }

            return values;
        }

        private InsertInfo ParseInsertBuilderValue<T>(IFromQuery fromQuery, LambdaExpression fieldSelector, SqlExpression valueExpression)
        {
            MemberExpression fieldSelectorBody;
            if (fieldSelector.Body is MemberExpression memberSelector)
            {
                fieldSelectorBody = memberSelector;
            }
            else if (fieldSelector.Body is UnaryExpression unarySelector)
            {
                if (unarySelector.NodeType == ExpressionType.Convert && unarySelector.Operand is MemberExpression convertMemberSelector)
                {
                    fieldSelectorBody = convertMemberSelector;
                }
                else
                {
                    throw new InvalidOperationException("Expected Convert(MemberExpression) in InsertBuilder.Value");
                }
            }
            else
            {
                throw new InvalidOperationException("Expected MemberExpression in InsertBuilder.Value");
            }

            var column = fromQuery.Columns.Where(c => c.MemberName == fieldSelectorBody.Member.Name).FirstOrDefault();
            if (column == null)
            {
                throw new InvalidOperationException("Not a valid column in InsertBuilder " + fieldSelectorBody.Member.Name);
            }

            return new InsertInfo()
            {
                Expression = valueExpression,
                SqlName = column.SqlName,
            };
        }

        public List<SqlOrderBy> ParseOrderByBuilder<T>(SqlSubQueryResult parentResult, LambdaExpression orderByExpr, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (orderByExpr.Body.NodeType != ExpressionType.Call)
            {
                throw new InvalidOperationException("Expression can only call OrderByBuilder<T>.Value()");
            }

            // Visit chained OrderByBuilder method calls backwards, adding in front
            var callExpression = (MethodCallExpression)orderByExpr.Body;
            var values = new List<SqlOrderBy>();
            while (true)
            {
                if (callExpression.Method.DeclaringType.GetGenericTypeDefinition() != typeof(OrderByBuilder<>))
                {
                    throw new InvalidOperationException("Expected OrderByBuilder<T> in expression");
                }

                if (callExpression.Method.Name == nameof(OrderByBuilder<bool>.Values))
                {
                    var builder = (OrderByBuilder<T>)ResolveConstant(callExpression.Arguments[0], out var argumentType);
                    var orderBys = new List<SqlOrderBy>();
                    foreach (var selector in builder.Selectors)
                    {
                        var selectorParameters = new Dictionary<string, SqlSubQueryResult>();
                        selectorParameters[selector.Selector.Parameters[0].Name] = parentResult;

                        var orderBySelector = ParseExpression(selector.Selector.Body, selectorParameters);
                        orderBys.Add(new SqlOrderBy()
                        {
                            Ascending = selector.Ascending,
                            SelectorExpression = orderBySelector,
                        });
                    }

                    values.InsertRange(0, orderBys);
                }
                else if (callExpression.Method.Name == nameof(OrderByBuilder<bool>.Value))
                {
                    var fieldSelectorUnary = (UnaryExpression)callExpression.Arguments[0];
                    var fieldSelector = (LambdaExpression)fieldSelectorUnary.Operand;
                    var selectorParameters = new Dictionary<string, SqlSubQueryResult>();
                    selectorParameters[fieldSelector.Parameters[0].Name] = parentResult;

                    var expr = ParseExpression(fieldSelector.Body, selectorParameters);
                    var ascendingExpression = callExpression.Arguments[1];
                    var ascending = (bool)ResolveConstant(ascendingExpression, out var ascendingType);
                    values.Insert(0, new SqlOrderBy()
                    {
                        Ascending = ascending,
                        SelectorExpression = expr,
                    });
                }
                else
                {
                    throw new InvalidOperationException("Insert expression can only call InsertBuilder<T>.Value() or .Values()");
                }

                if (callExpression.Object.NodeType == ExpressionType.Parameter)
                {
                    break;
                }

                callExpression = (MethodCallExpression)callExpression.Object;
            }

            return values;
        }

        private string RegisterConstant(object value)
        {
            var key = "p" + Constants.Count.ToString();
            Constants.Add(key, value);
            return key;
        }
    }
}
