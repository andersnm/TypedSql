using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypedSql
{
    public abstract class SqlBaseMaterializer
    {
        public abstract IEnumerable<T> ReadTypedReader<T>(IDataReader reader, List<SqlMember> selectMembers);

        protected bool IsScalarType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal) || type == typeof(byte[]) || IsEnumType(type)
                 || (IsNullable(type) && IsScalarType(typeInfo.GenericTypeArguments[0]));
        }

        protected bool IsNullable(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        protected bool IsEnumType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsEnum;
        }

        protected bool IsAnonymousType(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return type.Namespace == null &&
                typeInfo.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null
                // && Attribute.IsDefined(typeInfo, typeof(CompilerGeneratedAttribute), false)
                && typeInfo.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (typeInfo.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        protected bool IsNullOrdinals(IDataRecord reader, int ordinal, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (!reader.IsDBNull(ordinal + i))
                {
                    return false;
                }
            }

            return true;
        }

        protected int GetOrdinalCount(List<SqlMember> members)
        {
            var usedOrdinals = 0;
            foreach (var member in members)
            {
                if (member is SqlExpressionMember memberExpr && memberExpr.Expression is SqlTableExpression tableExpression)
                {
                    usedOrdinals += GetOrdinalCount(tableExpression.TableResult.Members);
                }
                else
                {
                    usedOrdinals++;
                }
            }

            return usedOrdinals;
        }

    }
}
