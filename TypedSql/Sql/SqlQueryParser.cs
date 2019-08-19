using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TypedSql {

    public class InsertInfo
    {
        public SqlExpression Expression { get; set; }
        public string SqlName { get; set; }
    }

    public class SqlAliasProvider {
        int counter = 0;

        public string CreateAlias() {
            var alias = "t" + counter.ToString();
            counter++;
            return alias;
        }
    }

    public class SqlQueryParser {
        readonly SqlAliasProvider AliasProvider;
        readonly Dictionary<string, SqlSubQueryResult> SubqueryParameters;

        public SqlQueryParser(SqlAliasProvider aliasProvider, Dictionary<string, SqlSubQueryResult> subqueryParameters) {
            AliasProvider = aliasProvider;
            SubqueryParameters = subqueryParameters;
        }

        public SqlQuery ParseQuery(Query query)
        {
            var result = ParseQuery(query, out var selectResult);
            result.SelectResult = selectResult;
            return result;
        }

        public SqlQuery ParseQuery(Query query, out SqlSubQueryResult selectResult) {
            if (query == null)
            {
                // SELECTs without FROM have empty result
                selectResult = new SqlSubQueryResult()
                {
                    Members = new List<SqlMember>(),
                };
                return new SqlQuery();
            }

            var fromQuery = query as IFromQuery;
            if (fromQuery != null) {
                return ParseFromQuery(fromQuery, out selectResult);
            }

            var whereQuery = query as IWhereQuery;
            if (whereQuery != null) {
                return ParseWhereQuery(whereQuery, out selectResult);
            }

            var joinQuery = query as IJoinQuery;
            if (joinQuery != null) {
                return ParseJoinQuery(joinQuery, out selectResult);
            }

            var groupByQuery = query as IGroupByQuery;
            if (groupByQuery != null) {
                return ParseGroupByQuery(query, groupByQuery, out selectResult);
            }

            var havingQuery = query as IHavingQuery;
            if (havingQuery != null) {
                return ParseHavingQuery(query, havingQuery, out selectResult);
            }
 
            /*var selectAggregateQuery = query as ISelectAggregateQuery;
            if (selectAggregateQuery != null)
            {
                return ParseSelectAggregateQuery(query, selectAggregateQuery, out selectResult);
            }*/

            var projectQuery = query as IProjectQuery;
            if (projectQuery != null)
            {
                return ParseProjectQuery(query, projectQuery, out selectResult);
            }

            var projectConstantQuery = query as IProjectConstantQuery;
            if (projectConstantQuery != null)
            {
                return ParseProjectConstantQuery(query, projectConstantQuery, out selectResult);
            }

            var selectQuery = query as ISelectQuery;
            if (selectQuery != null)
            {
                // Wråp in subquery
                return ParseSelectQuery(query, selectQuery, out selectResult);
            }

            var orderByQuery = query as IOrderByQuery;
            if (orderByQuery != null)
            {
                return ParseOrderByQuery(orderByQuery, out selectResult);
            }

            var offsetQuery = query as IOffsetQuery;
            if (offsetQuery != null)
            {
                return ParseOffsetQuery(offsetQuery, out selectResult);
            }

            var limitQuery = query as ILimitQuery;
            if (limitQuery != null)
            {
                return ParseLimitQuery(limitQuery, out selectResult);
            }

            throw new NotImplementedException("Unhandled query component");
        }

        SqlQuery ParseGroupByQuery(Query query, IGroupByQuery groupByQuery, out SqlSubQueryResult parentResult) {
            var result = ParseQuery(query.Parent, out var tempParentResult);

            var newExpression = groupByQuery.GroupExpression.Body as NewExpression;

            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            parameters[groupByQuery.GroupExpression.Parameters[0].Name] = tempParentResult;

            foreach (var argument in newExpression.Arguments)
            {
                result.GroupBys.Add(ParseExpression(argument, parameters));
            }

            var projectParameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            projectParameters[groupByQuery.ProjectExpression.Parameters[0].Name] = tempParentResult; // ctx
            projectParameters[groupByQuery.ProjectExpression.Parameters[1].Name] = tempParentResult;

            parentResult = new SqlSubQueryResult()
            {
                Members = ParseSelectExpression(groupByQuery.ProjectExpression, projectParameters),
            };

            return result;
        }

        SqlQuery ParseHavingQuery(Query query, IHavingQuery havingQuery, out SqlSubQueryResult parentResult) {
            var result = ParseQuery(query.Parent, out parentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            parameters[havingQuery.HavingExpression.Parameters[0].Name] = parentResult;

            result.Havings.Add(ParseExpression(havingQuery.HavingExpression.Body, parameters));
            return result;
        }

        SqlQuery ParseJoinQuery(IJoinQuery joinQuery, out SqlSubQueryResult parentResult) {
            var result = ParseQuery(joinQuery.Parent, out var tempParentResult);
            var joinParser = new SqlQueryParser(AliasProvider, SubqueryParameters);

            var joinFromSubQuery = joinParser.ParseQuery(joinQuery.JoinTable);

            if (joinQuery.JoinTable is IFromQuery)
            {
                ParseJoinTableQuery(tempParentResult, joinFromSubQuery, joinQuery, result, out parentResult);
                return result;
            }
            else
            {
                ParseJoinSubQuery(tempParentResult, joinFromSubQuery, joinQuery, result, out parentResult);
                return result;
            }
        }

        void ParseJoinSubQuery(SqlSubQueryResult parentResult, SqlQuery joinFromSubQuery, IJoinQuery joinQuery, SqlQuery result, out SqlSubQueryResult joinResult)
        {
            var joinAlias = AliasProvider.CreateAlias();

            // Translate fields from inside the subquery to fields rooted in the subquery usable from the outside
            var tempFromSubQuery = new SqlSubQueryResult()
            {
                Members = joinFromSubQuery.SelectResult.Members.Select(m => new SqlJoinFieldMember()
                {
                    JoinAlias = joinAlias,
                    SourceField = m,
                    MemberName = m.MemberName,
                    SqlName = m.SqlName,
                    SqlNameAlias = m.SqlNameAlias,
                    FieldType = m.FieldType,
                }).ToList<SqlMember>(),
            };

            // Parameters in both selector & join expressions: actx, a, bctx, b
            var outerParameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            outerParameters[joinQuery.JoinExpression.Parameters[0].Name] = parentResult;
            outerParameters[joinQuery.JoinExpression.Parameters[1].Name] = parentResult;
            outerParameters[joinQuery.JoinExpression.Parameters[2].Name] = tempFromSubQuery;
            outerParameters[joinQuery.JoinExpression.Parameters[3].Name] = tempFromSubQuery;

            var joinResultMembers = ParseSelectExpression(joinQuery.ResultExpression, outerParameters);

            var joinInfo = new SqlJoinSubQuery()
            {
                JoinAlias = joinAlias,
                JoinResult = new SqlSubQueryResult()
                {
                    Members = joinResultMembers,
                },
                JoinFrom = joinFromSubQuery,
                JoinExpression = ParseExpression(joinQuery.JoinExpression.Body, outerParameters),
                JoinType = joinQuery.JoinType,
            };

            result.Joins.Add(joinInfo);
            joinResult = joinInfo.JoinResult;
        }

        void ParseJoinTableQuery(SqlSubQueryResult parentResult, SqlQuery joinFromSubQuery, IJoinQuery joinQuery, SqlQuery result, out SqlSubQueryResult joinResult)
        {
            // Parameters in both selector & join expressions: actx, a, bctx, b
            var outerParameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            outerParameters[joinQuery.JoinExpression.Parameters[0].Name] = parentResult;
            outerParameters[joinQuery.JoinExpression.Parameters[1].Name] = parentResult;
            outerParameters[joinQuery.JoinExpression.Parameters[2].Name] = joinFromSubQuery.SelectResult;
            outerParameters[joinQuery.JoinExpression.Parameters[3].Name] = joinFromSubQuery.SelectResult;

            var joinResultMembers = ParseSelectExpression(joinQuery.ResultExpression, outerParameters);

            var joinInfo = new SqlJoinTable()
            {
                TableAlias = joinFromSubQuery.FromAlias,
                FromSource = joinFromSubQuery.From,
                JoinExpression = ParseExpression(joinQuery.JoinExpression.Body, outerParameters),
                JoinResult = new SqlSubQueryResult()
                {
                    Members = joinResultMembers,
                },
                JoinType = joinQuery.JoinType,
            };

            result.Joins.Add(joinInfo);

            joinResult = joinInfo.JoinResult;
        }

        SqlQuery ParseWhereQuery(IWhereQuery whereQuery, out SqlSubQueryResult parentResult) {
            var result = ParseQuery(whereQuery.Parent, out parentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);
            parameters[whereQuery.WhereExpression.Parameters[0].Name] = parentResult;

            result.Wheres.Add(ParseExpression(whereQuery.WhereExpression.Body, parameters));
            return result;
        }

        private SqlQuery ParseFromQuery(IFromQuery fromQuery, out SqlSubQueryResult selectResult) {
            var tableAlias = AliasProvider.CreateAlias();

            selectResult = new SqlSubQueryResult() {
                Members = new List<SqlMember>()
            };
            
            foreach (var column in fromQuery.Columns)
            {
                selectResult.Members.Add(new SqlTableFieldMember()
                {
                    MemberName = column.MemberName,
                    SqlName = column.SqlName,
                    SqlNameAlias = column.MemberName,
                    TableAlias = tableAlias,
                    TableType = fromQuery.TableType,
                    FieldType = column.OriginalType,
                });
            }

            return new SqlQuery()
            {
                From = new SqlFromTable()
                {
                    TableName = fromQuery.TableName,
                },
                FromAlias = tableAlias,
            };
        }

        private SqlQuery ParseProjectQuery(Query query, IProjectQuery projectQuery, out SqlSubQueryResult parentResult)
        {
            var result = ParseQuery(query.Parent, out var tempParentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);

            parameters[projectQuery.SelectExpression.Parameters[0].Name] = tempParentResult; // ctx
            parameters[projectQuery.SelectExpression.Parameters[1].Name] = tempParentResult; // item

            parentResult = new SqlSubQueryResult()
            {
                Members = ParseSelectExpression(projectQuery.SelectExpression, parameters)
            };

            return result;
        }

        private SqlQuery ParseProjectConstantQuery(Query query, IProjectConstantQuery projectQuery, out SqlSubQueryResult parentResult)
        {
            var result = ParseQuery(query.Parent, out var tempParentResult);

            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);

            parameters[projectQuery.SelectExpression.Parameters[0].Name] = tempParentResult; // ctx

            parentResult = new SqlSubQueryResult()
            {
                Members = ParseSelectExpression(projectQuery.SelectExpression, parameters)
            };

            return result;
        }

        private SqlQuery ParseSelectQuery(Query query, ISelectQuery selectQuery, out SqlSubQueryResult parentResult)
        {
            var joinAlias = AliasProvider.CreateAlias();
            var result = ParseQuery(query.Parent, out var tempParentResult);
            var parameters = new Dictionary<string, SqlSubQueryResult>(SubqueryParameters);

            parameters[selectQuery.SelectExpression.Parameters[0].Name] = tempParentResult; // ctx
            parameters[selectQuery.SelectExpression.Parameters[1].Name] = tempParentResult; // item

            result.SelectResult = new SqlSubQueryResult()
            {
                Members = ParseSelectExpression(selectQuery.SelectExpression, parameters)
            };

            parentResult = new SqlSubQueryResult()
            {
                Members = result.SelectResult.Members.Select(m => new SqlJoinFieldMember()
                {
                    JoinAlias = joinAlias,
                    SourceField = m,
                    MemberName = m.MemberName,
                    SqlName = m.SqlName,
                    SqlNameAlias = m.SqlNameAlias,
                    FieldType = m.FieldType,
                }).ToList<SqlMember>(),
            };

            return new SqlQuery()
            {
                From = new SqlFromSubQuery()
                {
                    FromQuery = result,
                },
                FromAlias = joinAlias,
            };
        }

        private SqlQuery ParseOrderByQuery(IOrderByQuery orderByQuery, out SqlSubQueryResult parentResult)
        {
            var result = ParseQuery(orderByQuery.Parent, out parentResult);

            var fieldSelector = orderByQuery.SelectorExpression;
            var fieldSelectorBody = (MemberExpression)fieldSelector.Body;

            var parameters = new Dictionary<string, SqlSubQueryResult>();
            parameters[fieldSelector.Parameters[0].Name] = parentResult;

            result.OrderBys.Add(new SqlOrderBy()
            {
                Ascending = orderByQuery.Ascending,
                SelectorExpression = ParseExpression(fieldSelectorBody, parameters),
            });
            
            return result;
        }

        private SqlQuery ParseOffsetQuery(IOffsetQuery offsetQuery, out SqlSubQueryResult parentResult)
        {
            var result = ParseQuery(offsetQuery.Parent, out parentResult);
            result.Offset = offsetQuery.OffsetIndex;
            return result;
        }

        private SqlQuery ParseLimitQuery(ILimitQuery limitQuery, out SqlSubQueryResult parentResult)
        {
            var result = ParseQuery(limitQuery.Parent, out parentResult);
            result.Limit = limitQuery.LimitIndex;
            return result;
        }

        private SqlExpression ParseExpression<T>(Expression<Func<T>> node) where T : struct
        {
            var parameters = new Dictionary<string, SqlSubQueryResult>();
            return ParseExpression(node.Body, parameters);
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

        private SqlExpression ParseFunctionCallExpression(MethodCallExpression call, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (call.Object == null && call.Method.DeclaringType == typeof(Function))
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
                        Op = ExpressionType.Quote, // ??? TODO
                        Left = arg0,
                        Right = arg1,
                    };
                }
                else
                {
                    throw new InvalidOperationException("Unsupported Function." + call.Method.Name);
                }
            }
            else
            {
                throw new Exception("No method calls except via Function.* - " + call.Method.Name);
            }
        }

        public SqlExpression ParseExpression(Expression node, Dictionary<string, SqlSubQueryResult> parameters) {
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

                if (member.Expression is ParameterExpression parameter)
                {
                    return GetParameterExpression(parameter.Name, member.Member.Name, parameters);
                }
                if (member.Expression is MemberExpression)
                {
                    var memberRef = ParseParameterMemberExpression(member, parameters);
                    if (memberRef != null)
                    {
                        if (memberRef is SqlTableFieldMember tableFieldRef)
                        {
                            return new SqlTableFieldExpression()
                            {
                                TableFieldRef = tableFieldRef,
                            };
                        }
                        else if (memberRef is SqlJoinFieldMember joinFieldRef)
                        {
                            return new SqlJoinFieldExpression()
                            {
                                JoinFieldRef = joinFieldRef,
                            };
                        }
                        else if (memberRef is SqlExpressionMember expressionRef)
                        {
                            return expressionRef.Expression;
                        }
                        else
                        {
                            throw new InvalidOperationException("Unhandled member ref in member expression");
                        }
                    }
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
                    Op = node.NodeType,
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
            else
            {
                throw new Exception("Unsupported expression " + node.NodeType);
            }
        }

        SqlMember ParseParameterMemberExpression(MemberExpression expression, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (expression.Expression is ParameterExpression parameter)
            {
                // Lookups on selector parameter
                var subQuery = parameters[parameter.Name];
                var subQueryMember = subQuery.Members.Where(m => m.MemberName == expression.Member.Name).First();
                return subQueryMember;
            }
            else
            if (expression.Expression is MemberExpression memberExpression)
            {
                var x = ParseParameterMemberExpression(memberExpression, parameters);

                if (x is SqlExpressionMember exprRef && exprRef.Expression is SqlTableExpression tableExpression)
                {
                    return tableExpression.TableResult.Members.Where(m => m.MemberName == expression.Member.Name).First();
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        SqlExpression GetConstantExpression(object value, Type type)
        {
            // var type = value.GetType();
            if (value is IEnumerable enumerable && !(value is string))
            {
                return new SqlConstantArrayExpression()
                {
                    Value = enumerable.Cast<object>().ToList(),
                };
            }

            if (value is bool b)
            {
                return new SqlPlaceholderExpression()
                {
                    Placeholder = new SqlPlaceholder<bool>() {
                        RawSql = b ? "1" : "0",
                        PlaceholderType = SqlPlaceholderType.RawSqlExpression,
                        Value = b,
                    },
                };
            }

            if (value == null)
            {
                return new SqlConstantExpression()
                {
                    ConstantType = type
                };
            }

            var key = RegisterConstant(value);
            return new SqlConstantExpression()
            {
                Value = "@" + key,
                ConstantType = type,
            };
        }

        object ResolveConstant(Expression expr, out Type constantType)
        {
            if (expr is MemberExpression memberExpr)
            {
                var thisValue = ResolveConstant(memberExpr.Expression, out var thisType);
                return GetObjectMember(thisValue, memberExpr.Member, out constantType);
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

        object GetObjectMember(object self, MemberInfo member, out Type type)
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

        SqlExpression GetParameterExpression(string parameterName, string memberName, Dictionary<string, SqlSubQueryResult> parameters)
        {
            var subQuery = parameters[parameterName];
            var subQueryMember = subQuery.Members.Where(m => m.MemberName == memberName).First();
            if ((subQueryMember is SqlTableFieldMember tableFieldRef))
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
            var members = new List<SqlMember>();
            if (selectExpression.Body.NodeType == ExpressionType.New)
            {
                var newExpression = selectExpression.Body as NewExpression;

                for (var i = 0; i < newExpression.Arguments.Count; i++)
                {
                    var argumentExpression = newExpression.Arguments[i];
                    var member = newExpression.Members[i];

                    var exprResult = ParseExpression(argumentExpression, parameters);
                    ParseSelectNewExpression(exprResult, member, members);
                }
            }
            else if (selectExpression.Body.NodeType == ExpressionType.Parameter)
            {
                var paramExpr = selectExpression.Body as ParameterExpression;
                var queryObject = parameters[paramExpr.Name];
                return queryObject.Members;
            }
            else if (selectExpression.Body.NodeType == ExpressionType.MemberInit) {
                var newExpression = selectExpression.Body as MemberInitExpression;

                for (var i = 0; i < newExpression.Bindings.Count; i++)
                {
                    var binding = newExpression.Bindings[i];
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
            }
            else if (selectExpression.Body is MemberExpression memberExpression)
            {
                // Select all members in a table
                var memberRef = ParseParameterMemberExpression(memberExpression, parameters);
                if (memberRef is SqlExpressionMember memberExprRef && memberExprRef.Expression is SqlTableExpression memberRefExprTable)
                {
                    return memberRefExprTable.TableResult.Members;
                }
                else
                {
                    // TODO: allow to select a single field
                    throw new ArgumentException("Select Member Expression should refer to a table");
                }
            }
            else
            {
                throw new ArgumentException("Select expression should be a LambdaDelegate whose Body property is a New, MemberAccess, MemberInit or Member Expression");
            }

            return members;
        }

        void ParseSelectNewExpression(SqlExpression exprResult, MemberInfo member, List<SqlMember> members)
        {
            if (exprResult is SqlTableFieldExpression subQueryExpr)
            {
                members.Add(new SqlExpressionMember()
                {
                    MemberName = member.Name,
                    SqlName = member.Name,
                    SqlNameAlias = member.Name,
                    Expression = subQueryExpr,
                    FieldType = subQueryExpr.GetExpressionType(),
                });
            }
            else if (exprResult is SqlJoinFieldExpression joinExpr)
            {
                members.Add(joinExpr.JoinFieldRef);
            }
            else if (exprResult is SqlConstantExpression || exprResult is SqlCastExpression || exprResult is SqlNegateExpression || exprResult is SqlBinaryExpression || exprResult is SqlPlaceholderExpression || exprResult is SqlCallExpression || exprResult is SqlConditionalExpression || exprResult is SqlTableExpression)
            {
                members.Add(new SqlExpressionMember()
                {
                    Expression = exprResult,
                    MemberName = member.Name,
                    SqlName = member.Name,
                    SqlNameAlias = member.Name,
                    FieldType = exprResult.GetExpressionType(),
                });
            }
            else
            {
                throw new InvalidOperationException("Unable to parse select new expression");
            }
        }

        public List<InsertInfo> ParseInsertBuilder(IFromQuery fromQuery, LambdaExpression insertExpr, Dictionary<string, SqlSubQueryResult> parameters)
        {
            if (insertExpr.Body.NodeType != ExpressionType.Call)
            {
                throw new InvalidOperationException("Insert expression can only call InsertBuilder<T>.Value()");
            }

            var callExpression = (MethodCallExpression)insertExpr.Body;
            if (callExpression.Method.Name != nameof(InsertBuilder<bool>.Value) || callExpression.Method.DeclaringType.GetGenericTypeDefinition() != typeof(InsertBuilder<>))
            {
                throw new InvalidOperationException("Insert expression can only call InsertBuilder<T>.Value()");
            }

            var values = new List<InsertInfo>();
            while (true)
            {
                var fieldSelectorUnary = (UnaryExpression)callExpression.Arguments[0];
                var fieldSelector = (LambdaExpression)fieldSelectorUnary.Operand;
                var fieldSelectorBody = (MemberExpression)fieldSelector.Body;

                var column = fromQuery.Columns.Where(c => c.MemberName == fieldSelectorBody.Member.Name).FirstOrDefault();
                if (column == null)
                {
                    throw new InvalidOperationException("Not a valid column in InsertBuilder " + fieldSelectorBody.Member.Name);
                }

                var valueExpression = callExpression.Arguments[1];
                var memberValueExpression = ParseExpression(valueExpression, parameters);

                values.Add(new InsertInfo()
                {
                    Expression = memberValueExpression,
                    SqlName = column.SqlName,
                });

                if (callExpression.Object.NodeType == ExpressionType.Parameter)
                {
                    break;
                }

                callExpression = (MethodCallExpression)callExpression.Object;
            }

            return values;
        }

        public Dictionary<string, object> Constants = new Dictionary<string, object>();

        string RegisterConstant(object value)
        {
            var key = "p" + Constants.Count.ToString();
            Constants.Add(key, value);
            return key;
        }
    }
}
