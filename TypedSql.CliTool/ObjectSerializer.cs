using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TypedSql.CliTool
{
    public class ObjectSerializer
    {
        public static void SerializeObject(object obj, int indent, StringBuilder writer)
        {
            if (obj == null)
            {
                writer.Append("null");
                return;
            }

            // TODO: check if object exists in a refmap, and print only the refname
            // => register all columns and tables? and lists?

            var type = obj.GetType();
            if (IsList(type.GetTypeInfo()))
            {
                var itemType = type.GetTypeInfo().GetGenericArguments()[0];
                writer.AppendLine("new List<" + itemType.Name + ">()");
                var e = (IEnumerable)obj;
                writer.AppendLine(Indent(indent) + "{");
                foreach (var item in e)
                {
                    writer.Append(Indent(indent + 1));
                    SerializeObject(item, indent + 1, writer);
                    writer.AppendLine(",");
                }
                writer.Append(Indent(indent) + "}");
                return;
            }

            switch (obj)
            {
                case Type typeValue:
                    writer.Append("typeof(" + typeValue.Name + ")");
                    return;
                case bool boolValue:
                    writer.Append(boolValue ? "true" : "false");
                    return;
                case int intValue:
                    writer.Append(intValue.ToString());
                    return;
                case uint uintValue:
                    writer.Append(uintValue.ToString());
                    return;
                case string stringValue:
                    // TODO: escape
                    writer.Append("\"" + stringValue.Replace("\"", "\\\"") + "\"");
                    return;
            }

            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsClass)
            {
                throw new InvalidOperationException("Expected class type at this point " + type.Name);
            }

            writer.AppendLine("new " + type.Name + "()");
            writer.AppendLine(Indent(indent) + "{");
            foreach (var propertyInfo in typeInfo.GetProperties())
            {
                var propertyValue = propertyInfo.GetGetMethod().Invoke(obj, new object[0]);
                if (propertyValue == null || propertyValue.Equals(GetDefault(propertyValue.GetType()))) {
                    continue;
                }
                writer.Append(Indent(indent + 1) + propertyInfo.Name + " = ");
                SerializeObject(propertyValue, indent + 1, writer);
                writer.AppendLine(",");
            }
            writer.Append(Indent(indent) + "}");
        }

        static object GetDefault(Type t)
        {
            return typeof(ObjectSerializer).GetMethod(nameof(GetDefaultGeneric), BindingFlags.NonPublic|BindingFlags.Static).MakeGenericMethod(t).Invoke(null, null);
        }

        static T GetDefaultGeneric<T>()
        {
            return default(T);
        }

        static bool IsList(TypeInfo typeInfo)
        {
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(List<>);
        }

        static string Indent(int indent)
        {
            return string.Join("", Enumerable.Repeat(" ", indent * 4));
        }
    }
}
