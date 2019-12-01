using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace TypedSql
{
    [ExcludeFromCodeCoverage]
    internal class DefaultMaterializer : SqlBaseMaterializer
    {
        public override IEnumerable<T> ReadTypedReader<T>(IDataReader reader, List<SqlMember> selectMembers)
        {
            while (reader.Read())
            {
                var row = ReadObject<T>(reader, selectMembers);
                yield return row;
            }
        }

        private T ReadObject<T>(IDataRecord reader, List<SqlMember> selectMembers)
        {
            var propertyType = typeof(T);

            if (IsScalarType(propertyType))
            {
                return (T)ReadSimpleValue(reader, propertyType, 0, out var _);
            }

            return (T)ReadComplexValue(reader, propertyType, selectMembers, 0, out var _);
        }

        private object ReadComplexValue(IDataRecord reader, Type propertyType, List<SqlMember> members, int ordinal, out int usedOrdinals)
        {
            usedOrdinals = GetOrdinalCount(members);
            if (IsNullOrdinals(reader, ordinal, usedOrdinals))
            {
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

        private object ReadSimpleValue(IDataRecord reader, Type propertyType, int ordinal, out int usedOrdinals)
        {
            var isNull = reader.IsDBNull(ordinal);
            var value = isNull ? null : reader.GetValue(ordinal);

            usedOrdinals = 1;
            if (IsNullable(propertyType))
            {
                if (value == null || value == DBNull.Value)
                {
                    return Activator.CreateInstance(propertyType, null);
                }

                value = ChangeTypeOrEnum(value, Nullable.GetUnderlyingType(propertyType));
                return Activator.CreateInstance(propertyType, value);
            }
            else
            {
                return ChangeTypeOrEnum(value, propertyType);
            }
        }

        private object ReadMemberValue(IDataRecord reader, SqlMember member, int ordinal, out int usedOrdinals)
        {
            if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
            {
                return ReadComplexValue(reader, member.MemberInfo.PropertyType, tableExpression.TableResult.Members, ordinal, out usedOrdinals);
            }

            return ReadSimpleValue(reader, member.MemberInfo.PropertyType, ordinal, out usedOrdinals);
        }

        private object ChangeTypeOrEnum(object value, Type conversionType)
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
