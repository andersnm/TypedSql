using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace TypedSql
{
    internal class CompiledMaterializer : SqlBaseMaterializer
    {

        private static readonly MethodInfo GetStringFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetString), new Type[] { typeof(int) });
        private static readonly MethodInfo GetIntFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetInt32), new Type[] { typeof(int) });
        private static readonly MethodInfo GetDecimalFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetDecimal), new Type[] { typeof(int) });
        private static readonly MethodInfo GetBooleanFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetBoolean), new Type[] { typeof(int) });
        private static readonly MethodInfo GetDateTimeFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetDateTime), new Type[] { typeof(int) });
        private static readonly MethodInfo GetDoubleFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetDouble), new Type[] { typeof(int) });
        private static readonly MethodInfo GetFloatFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetFloat), new Type[] { typeof(int) });
        private static readonly MethodInfo GetByteFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetByte), new Type[] { typeof(int) });
        private static readonly MethodInfo GetShortFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetInt16), new Type[] { typeof(int) });
        private static readonly MethodInfo GetLongFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.GetInt64), new Type[] { typeof(int) });
        private static readonly MethodInfo IsDBNullFunction = typeof(IDataRecord).GetTypeInfo().GetMethod(nameof(IDataRecord.IsDBNull), new Type[] { typeof(int) });
        private static readonly MethodInfo ReadBytesFunction = typeof(CompiledMaterializer).GetTypeInfo().GetMethod(nameof(CompiledMaterializer.ReadBytes), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo IsNullOrdinalsFunction = typeof(CompiledMaterializer).GetTypeInfo().GetMethod(nameof(CompiledMaterializer.IsNullOrdinals), BindingFlags.NonPublic | BindingFlags.Instance);

        public override IEnumerable<T> ReadTypedReader<T>(IDataReader reader, List<SqlMember> selectMembers)
        {
            var mapper = Compile<T>(selectMembers);

            while (reader.Read())
            {
                var row = mapper(reader);
                yield return row;
            }
        }

        private Func<IDataRecord, T> Compile<T>(List<SqlMember> selectMembers)
        {
            var type = typeof(T);
            var recordParameter = Expression.Parameter(typeof(IDataRecord), "record");

            if (IsScalarType(type))
            {
                var body = CompileSimpleValue(type, recordParameter, 0);
                return Expression.Lambda<Func<IDataRecord, T>>(body, recordParameter).Compile();
            }
            else
            {
                var body = CompileComplexValue(type, selectMembers, recordParameter, 0, out _);
                return Expression.Lambda<Func<IDataRecord, T>>(body, recordParameter).Compile();
            }
        }

        private Expression CompileSimpleValue(Type propertyType, Expression recordParameter, int ordinal)
        {
            var typeInfo = propertyType.GetTypeInfo();
            if (IsNullable(propertyType))
            {
                var nullableDefault = Expression.Default(propertyType);
                var nullableValueConstructor = typeInfo.GetConstructor(new[] { Nullable.GetUnderlyingType(propertyType) });
                var isNullExpression = Expression.Call(recordParameter, IsDBNullFunction, Expression.Constant(ordinal, typeof(int)));
                return Expression.Condition(isNullExpression,
                    nullableDefault,
                    Expression.New(nullableValueConstructor,
                        CompileBaseScalar(Nullable.GetUnderlyingType(propertyType), recordParameter, ordinal)
                    )
                );
            }
            else
            {
                return CompileBaseScalar(propertyType, recordParameter, ordinal);
            }
        }

        private Expression CompileComplexValue(Type propertyType, List<SqlMember> members, Expression recordParameter, int ordinal, out int usedOrdinals)
        {
            usedOrdinals = GetOrdinalCount(members);

            var isNullExpression = Expression.Call(Expression.Constant(this, typeof(CompiledMaterializer)), IsNullOrdinalsFunction,
                recordParameter,
                Expression.Constant(ordinal, typeof(int)),
                Expression.Constant(usedOrdinals, typeof(int)));

            Expression ifNotNullExpression;
            if (IsAnonymousType(propertyType))
            {
                var constructorArguments = new List<Expression>();

                foreach (var objectMember in members)
                {
                    var memberValue = CompileMemberValue(objectMember, recordParameter, ordinal, out var usedMemberOrdinals);
                    constructorArguments.Add(memberValue);
                    ordinal += usedMemberOrdinals;
                }

                var constructor = propertyType.GetTypeInfo().GetConstructors()[0];
                ifNotNullExpression = Expression.New(constructor, constructorArguments);
            }
            else
            {
                var bindings = new List<MemberBinding>();
                foreach (var objectMember in members)
                {
                    var memberValue = CompileMemberValue(objectMember, recordParameter, ordinal, out var usedMemberOrdinals);
                    bindings.Add(Expression.Bind(objectMember.MemberInfo, memberValue));
                    ordinal += usedMemberOrdinals;
                }

                ifNotNullExpression = Expression.MemberInit(Expression.New(propertyType), bindings);
            }

            return Expression.Condition(isNullExpression,
                Expression.Constant(null, propertyType),
                ifNotNullExpression);
        }

        private Expression CompileMemberValue(SqlMember member, Expression recordParameter, int ordinal, out int usedOrdinals)
        {
            if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
            {
                return CompileComplexValue(member.MemberInfo.PropertyType, tableExpression.TableResult.Members, recordParameter, ordinal, out usedOrdinals);
            }

            usedOrdinals = 1;
            return CompileSimpleValue(member.MemberInfo.PropertyType, recordParameter, ordinal);
        }

        private Expression CompileBaseScalar(Type propertyType, Expression recordParameter, int ordinal)
        {
            if (propertyType == typeof(byte[]))
            {
                return Expression.Call(Expression.Constant(this, typeof(CompiledMaterializer)), ReadBytesFunction, recordParameter, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(string))
            {
                var isNullExpression = Expression.Call(recordParameter, IsDBNullFunction, Expression.Constant(ordinal, typeof(int)));
                return Expression.Condition(isNullExpression,
                    Expression.Constant(null, typeof(string)),
                    Expression.Call(recordParameter, GetStringFunction, Expression.Constant(ordinal, typeof(int))));
            }
            else if (propertyType == typeof(byte))
            {
                return Expression.Call(recordParameter, GetByteFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(short))
            {
                return Expression.Call(recordParameter, GetShortFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(int))
            {
                return Expression.Call(recordParameter, GetIntFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(long))
            {
                return Expression.Call(recordParameter, GetLongFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(decimal))
            {
                return Expression.Call(recordParameter, GetDecimalFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(bool))
            {
                return Expression.Call(recordParameter, GetBooleanFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(DateTime))
            {
                return Expression.Call(recordParameter, GetDateTimeFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(double))
            {
                return Expression.Call(recordParameter, GetDoubleFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (propertyType == typeof(float))
            {
                return Expression.Call(recordParameter, GetFloatFunction, Expression.Constant(ordinal, typeof(int)));
            }
            else if (IsEnumType(propertyType))
            {
                var enumBaseType = Enum.GetUnderlyingType(propertyType);
                return Expression.Convert(CompileBaseScalar(enumBaseType, recordParameter, ordinal), propertyType);
            }
            else
            {
                throw new InvalidOperationException("Base scalar not supported " + propertyType.Name);
            }
        }

        private byte[] ReadBytes(IDataRecord reader, int ordinal)
        {
            var length = (int)reader.GetBytes(ordinal, 0, null, 0, 0);
            var buffer = new byte[length];
            reader.GetBytes(ordinal, 0, buffer, 0, length);
            return buffer;
        }
    }
}
