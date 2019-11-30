using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypedSql
{
    public static class DataExtensions
    {
        public static IEnumerable<T> ReadTypedReader<T>(this IDataReader reader, List<SqlMember> selectMembers)
        {
            while (reader.Read())
            {
                var row = ReadObject<T>(reader, selectMembers);
                yield return row;
            }
        }

        static bool IsScalarType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal) || type == typeof(byte[]) || IsEnumType(type);
        }

        static bool IsNullable(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static bool IsEnumType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsEnum;
        }

        static bool IsAnonymousType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return type.Namespace == null &&
                typeInfo.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null
                // && Attribute.IsDefined(typeInfo, typeof(CompilerGeneratedAttribute), false)
                && typeInfo.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (typeInfo.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        static T ReadObject<T>(IDataRecord reader, List<SqlMember> selectMembers)
        {
            var propertyType = typeof(T);
            var typeInfo = propertyType.GetTypeInfo();

            if (IsScalarType(propertyType))
            {
                return (T)ReadSimpleValue(reader, propertyType, 0, out var _);
            }
            else if (IsNullable(propertyType) && IsScalarType(typeInfo.GenericTypeArguments[0]))
            {
                return (T)ReadSimpleValue(reader, propertyType, 0, out var _);
            }

            return (T)ReadComplexValue(reader, propertyType, selectMembers, 0, out var _);
        }

        static bool IsNullObject(IDataRecord reader, List<SqlMember> members, int ordinal, out int usedOrdinals)
        {
            var isNullObject = true;
            usedOrdinals = 0;
            foreach (var member in members)
            {
                if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
                {
                    if (!IsNullObject(reader, tableExpression.TableResult.Members, ordinal, out var usedMemberOrdinals))
                    {
                        isNullObject = false;
                    }

                    ordinal += usedMemberOrdinals;
                    usedOrdinals += usedMemberOrdinals;
                }
                else
                {
                    if (!reader.IsDBNull(ordinal))
                    {
                        isNullObject = false;
                    }

                    ordinal++;
                    usedOrdinals++;
                }
            }

            return isNullObject;
        }

        static object ReadComplexValue(IDataRecord reader, Type propertyType, List<SqlMember> members, int ordinal, out int usedOrdinals)
        {
            if (IsNullObject(reader, members, ordinal, out int nullOrdinals))
            {
                usedOrdinals = nullOrdinals;
                return null;
            }

            if (IsAnonymousType(propertyType))
            {
                var constructorArguments = new List<object>();
                usedOrdinals = 0;

                foreach (var objectMember in members)
                {
                    var memberValue = ReadMemberValue(reader, objectMember, ordinal, out var usedMemberOrdinals);
                    constructorArguments.Add(memberValue);
                    usedOrdinals += usedMemberOrdinals;
                    ordinal += usedMemberOrdinals;
                }

                var obj = Activator.CreateInstance(propertyType, constructorArguments.ToArray());
                return obj;
            }
            else
            {
                var obj = Activator.CreateInstance(propertyType);
                usedOrdinals = 0;
                foreach (var objectMember in members)
                {
                    var memberValue = ReadMemberValue(reader, objectMember, ordinal, out var usedMemberOrdinals);
                    objectMember.MemberInfo.SetValue(obj, memberValue);
                    usedOrdinals += usedMemberOrdinals;
                    ordinal += usedMemberOrdinals;
                }

                return obj;
            }
        }

        static object ReadSimpleValue(IDataRecord reader, Type propertyType, int ordinal, out int usedOrdinals)
        {
            var typeInfo = propertyType.GetTypeInfo();
            var isNull = reader.IsDBNull(ordinal);
            var value = isNull ? null : reader.GetValue(ordinal);

            if (IsScalarType(propertyType))
            {
                usedOrdinals = 1;
                return ChangeTypeOrEnum(value, propertyType);
            }
            else if (IsNullable(propertyType) && IsScalarType(typeInfo.GenericTypeArguments[0]))
            {
                usedOrdinals = 1;

                if (value == null || value == DBNull.Value)
                {
                    return Activator.CreateInstance(propertyType, null);
                }

                var underlyingValue = ChangeTypeOrEnum(value, Nullable.GetUnderlyingType(propertyType));
                return Activator.CreateInstance(propertyType, underlyingValue);
            }

            throw new InvalidOperationException("Expected scalar in ReadSimpleValue");
        }

        static object ReadMemberValue(IDataRecord reader, SqlMember member, int ordinal, out int usedOrdinals)
        {
            if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
            {
                return ReadComplexValue(reader, member.MemberInfo.PropertyType, tableExpression.TableResult.Members, ordinal, out usedOrdinals);
            }

            return ReadSimpleValue(reader, member.MemberInfo.PropertyType, ordinal, out usedOrdinals);
        }

        static object ChangeTypeOrEnum(object value, Type conversionType)
        {
            if (IsEnumType(conversionType))
            {
                return Enum.ToObject(conversionType, value);
            }
            else
            {
                // NOTE: if this throws, check for left joins not casting to nullable
                return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
            }
        }
    }
}
