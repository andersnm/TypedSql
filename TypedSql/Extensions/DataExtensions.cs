using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TypedSql
{
    public static class DataExtensions
    {
        public static IEnumerable<T> ReadTypedReader<T>(this IDataReader reader)
        {
            while (reader.Read())
            {
                var row = ReadObject<T>(reader);
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

        static T ReadObject<T>(IDataRecord reader)
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
                    return ReadRowConstructor<T>(constructor, reader);
                }
            }

            // Otherwise choose the constructor without arguments
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    return ReadRowObject<T>(constructor, reader);
                }
            }

            throw new InvalidOperationException("Could not find a constructor for " + typeof(T));
        }

        static T ReadRowConstructor<T>(ConstructorInfo constructor, IDataRecord reader)
        {
            var typeInfo = typeof(T).GetTypeInfo();
            var properties = typeInfo.GetProperties();

            var parameters = new List<object>();
            foreach (var property in properties)
            {
                var value = GetValue(reader, property);
                parameters.Add(value);
            }

            return (T)constructor.Invoke(parameters.ToArray());
        }

        static T ReadRowObject<T>(ConstructorInfo constructor, IDataRecord reader)
        {
            var row = (T)constructor.Invoke(new object[0]);
            var typeInfo = typeof(T).GetTypeInfo();
            var properties = typeInfo.GetProperties();
            foreach (var property in properties)
            {
                var value = GetValue(reader, property);
                property.SetValue(row, value);

            }
            return row;
        }

        static object GetValue(IDataRecord reader, PropertyInfo property)
        {
            var ordinal = reader.GetOrdinal(property.Name);
            var isNull = reader.IsDBNull(ordinal);
            var value = isNull ? null : reader.GetValue(ordinal);
            var nullable = IsNullable(property.PropertyType);

            if (nullable)
            {
                if (value == null || value == DBNull.Value)
                {
                    return Activator.CreateInstance(property.PropertyType, null);
                }

                var underlyingValue = Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType));
                return Activator.CreateInstance(property.PropertyType, underlyingValue);
            }
            else
            {
                // NOTE: if this throws, check for left joins not casting to nullable
                return Convert.ChangeType(value, property.PropertyType);
            }
        }
    }
}
