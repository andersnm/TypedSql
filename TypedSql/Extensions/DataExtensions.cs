using System;
using System.Collections.Generic;
using System.Data;
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
            return typeInfo.IsPrimitive || type == typeof(string) || type == typeof(DateTime);
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
            var type = typeof(T);
            var typeInfo = typeof(T).GetTypeInfo();
            var constructors = typeInfo.GetConstructors();
            var properties = typeInfo.GetProperties();

            if (IsScalarType(type))
            {
                if (reader.FieldCount != 1)
                {
                    throw new InvalidOperationException("Scalar ReadObject<T> has more than one result");
                }

                return (T)Convert.ChangeType(reader.GetValue(0), type);
            }
            else if (IsNullable(type) && IsScalarType(typeInfo.GenericTypeArguments[0]))
            {
                if (reader.FieldCount != 1)
                {
                    throw new InvalidOperationException("Scalar nullable ReadObject<T> has more than one result");
                }

                object value = reader.GetValue(0);
                if (value == null || value == DBNull.Value)
                    return default(T);
                return (T)Activator.CreateInstance(type, value);
            }
            else if (IsAnonymousType(type) && constructors.Length == 1) {
                var constructor = constructors[0];
                var parameters = constructor.GetParameters();
                if (parameters.Length == properties.Length)
                {
                    return ReadRowConstructor<T>(constructor, reader, selectMembers);
                }
            }

            // Otherwise choose the constructor without arguments
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    return ReadRowObject<T>(constructor, reader, selectMembers);
                }
            }

            throw new InvalidOperationException("Could not find a constructor for " + typeof(T));
        }

        static T ReadRowConstructor<T>(ConstructorInfo constructor, IDataRecord reader, List<SqlMember> selectMembers)
        {
            var ordinal = 0;
            var parameters = new List<object>();
            foreach (var member in selectMembers)
            {
                var value = GetValue(reader, member, ordinal, out var usedOrdinals);
                parameters.Add(value);
                ordinal += usedOrdinals;
            }

            return (T)constructor.Invoke(parameters.ToArray());
        }

        static T ReadRowObject<T>(ConstructorInfo constructor, IDataRecord reader, List<SqlMember> selectMembers)
        {
            var ordinal = 0;
            var row = (T)constructor.Invoke(new object[0]);
            foreach (var member in selectMembers)
            {
                var value = GetValue(reader, member, ordinal, out var usedOrdinals);
                member.MemberInfo.SetValue(row, value);
                ordinal += usedOrdinals;
            }

            return row;
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

        static object GetValue(IDataRecord reader, SqlMember member, int ordinal, out int usedOrdinals)
        {
            var propertyType = member.MemberInfo.PropertyType;

            // NOTE: Cannot tell the difference between a null object and an object where all fields are null!
            // Assuming null object if all fields are null
            if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
            {
                if (IsNullObject(reader, tableExpression.TableResult.Members, ordinal, out int nullOrdinals))
                {
                    usedOrdinals = nullOrdinals;
                    return null;
                }

                if (IsAnonymousType(propertyType))
                {
                    var constructorArguments = new List<object>();
                    usedOrdinals = 0;
                    foreach (var objectMember in tableExpression.TableResult.Members)
                    {
                        var memberValue = GetValue(reader, objectMember, ordinal, out var usedMemberOrdinals);
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
                    foreach (var objectMember in tableExpression.TableResult.Members)
                    {
                        var memberValue = GetValue(reader, objectMember, ordinal, out var usedMemberOrdinals);
                        objectMember.MemberInfo.SetValue(obj, memberValue);
                        usedOrdinals += usedMemberOrdinals;
                        ordinal += usedMemberOrdinals;
                    }

                    return obj;
                }
            }

            var isNull = reader.IsDBNull(ordinal);
            var value = isNull ? null : reader.GetValue(ordinal);
            var nullable = IsNullable(propertyType);

            if (nullable)
            {
                usedOrdinals = 1;
                if (value == null || value == DBNull.Value)
                {
                    return Activator.CreateInstance(propertyType, null);
                }

                var underlyingValue = ChangeTypeOrEnum(value, Nullable.GetUnderlyingType(propertyType));
                return Activator.CreateInstance(propertyType, underlyingValue);
            }
            else
            {
                usedOrdinals = 1;
                return ChangeTypeOrEnum(value, propertyType);
            }
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
                return Convert.ChangeType(value, conversionType);
            }
        }
    }
}
