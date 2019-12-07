using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TypedSql.Schema;

namespace TypedSql
{
    public class SelectorValue
    {
        public LambdaExpression Selector { get; set; }
        public object Value { get; set; }
    }

    public interface IInsertBuilder
    {
        List<SelectorValue> Selectors { get; }
        Type BuilderType { get; }
    }

    public class InsertBuilder<T> : IInsertBuilder
    {
        public List<SelectorValue> Selectors { get; } = new List<SelectorValue>();
        public Type BuilderType { get; } = typeof(T);

        public InsertBuilder<T> Value<TValueType>(Expression<Func<T, TValueType>> selector, TValueType value)
        {
            Selectors.Add(new SelectorValue() { Selector = selector, Value = value });
            return this;
        }

        public InsertBuilder<T> Values(InsertBuilder<T> other)
        {
            Selectors.AddRange(other.Selectors);
            return this;
        }
    }

    public interface IFromQuery
    {
        Type TableType { get; }
        string TableName { get; }
        List<Column> Columns { get; }
        List<ForeignKey> ForeignKeys { get; }
        List<Index> Indices { get; }
        DatabaseContext Context { get; }
    }

    public class FromQuery<T> : FlatQuery<T, T>, IFromQuery
        where T : new()
    {
        public Type TableType { get => typeof(T); }
        public string TableName { get; private set; }
        public List<Column> Columns { get; } = new List<Column>();
        public List<ForeignKey> ForeignKeys { get; } = new List<ForeignKey>();
        public List<Index> Indices { get; } = new List<Index>();
        public DatabaseContext Context { get; }
        internal List<T> Data { get; } = new List<T>();
        internal int Identity { get; set; } = 1;

        public FromQuery(DatabaseContext context)
            : base(null)
        {
            Context = context;
            ParseAttributes();
        }

        internal override IEnumerable<T> InMemorySelect(IQueryRunner runner)
        {
            FromRowMapping = Data.ToDictionary(x => x);
            return Data;
        }

        internal void InsertImpl(InsertBuilder<T> builder, out int identity)
        {
            var itemType = typeof(T);
            var typeInfo = itemType.GetTypeInfo();
            var itemObject = new T();
            var identityPrimaryKey = Columns.Where(c => c.PrimaryKey && c.PrimaryKeyAutoIncrement).FirstOrDefault();

            if (identityPrimaryKey != null)
            {
                var propertyInfo = typeInfo.GetProperty(identityPrimaryKey.MemberName);
                propertyInfo.SetValue(itemObject, Identity);
            }

            UpdateObject(itemObject, builder.BuilderType, builder);
            Data.Add(itemObject);
            Identity++;

            identity = Identity - 1;
        }

        private MemberExpression GetSelectorMemberExpression(LambdaExpression fieldSelector, out Type selectorType)
        {
            // NOTE: duplicated from ParseInsertBuilderValue
            MemberExpression fieldSelectorBody;
            if (fieldSelector.Body is MemberExpression memberSelector)
            {
                fieldSelectorBody = memberSelector;
                selectorType = memberSelector.Type;
            }
            else if (fieldSelector.Body is UnaryExpression unarySelector)
            {
                if (unarySelector.NodeType == ExpressionType.Convert && unarySelector.Operand is MemberExpression convertMemberSelector)
                {
                    fieldSelectorBody = convertMemberSelector;
                    selectorType = unarySelector.Type;
                }
                else
                {
                    throw new InvalidOperationException("Expected Convert(MemberExpression) in selector");
                }
            }
            else
            {
                throw new InvalidOperationException("Expected MemberExpression in selector");
            }

            return fieldSelectorBody;
        }

        internal void UpdateObject(T itemObject, Type fromType, InsertBuilder<T> fromObject)
        {
            // Use the InsertBuilder to update an instance of the type of the backing table
            var typeInfo = typeof(T).GetTypeInfo();

            foreach (var fromSelector in fromObject.Selectors)
            {
                var sl = GetSelectorMemberExpression(fromSelector.Selector, out var selectorType);
                var itemPropertyInfo = typeInfo.GetProperty(sl.Member.Name);
                if (itemPropertyInfo == null)
                {
                    throw new InvalidOperationException("Can not set property '" + sl.Member.Name + "' on type " + typeof(T).Name);
                }

                var schemaColumn = Columns.Where(c => c.MemberName == itemPropertyInfo.Name).FirstOrDefault();
                if (schemaColumn == null)
                {
                    throw new InvalidOperationException("Can not set inmemory table column " + itemPropertyInfo.Name);
                }

                if (schemaColumn.PrimaryKey && schemaColumn.PrimaryKeyAutoIncrement)
                {
                    throw new InvalidOperationException("Cannot set the value on an identity primary key");
                }

                var value = fromSelector.Value;

                // Workaround for lambda expression like "...Value(x => x.ByteValue, 1)" which is
                // internally represented as "...Value(x => Convert(x.ByteValue, Int32), (int)1)"
                if (value != null && sl.Type != selectorType)
                {
                    value = Convert.ChangeType(value, sl.Type);
                }

                itemPropertyInfo.SetValue(itemObject, value);
            }
        }

        internal int DeleteObject(T itemObject)
        {
            return Data.Remove(itemObject) ? 1 : 0;
        }

        private void ParseAttributes()
        {
            var typeInfo = typeof(T).GetTypeInfo();
            var tableAttr = typeInfo.GetCustomAttribute<SqlTableAttribute>();
            if (tableAttr != null)
            {
                TableName = tableAttr.Name;
            }
            else
            {
                TableName = typeof(T).Name;
            }

            var properties = typeInfo.GetProperties();

            foreach (var property in properties)
            {
                var primaryKeyAttribute = property.GetCustomAttribute<PrimaryKeyAttribute>();
                var stringAttribute = property.GetCustomAttribute<SqlStringAttribute>();
                var decimalAttribute = property.GetCustomAttribute<SqlDecimalAttribute>();
                var nullableAttribute = property.GetCustomAttribute<SqlNullableAttribute>();

                var propertyTypeInfo = property.PropertyType.GetTypeInfo();
                var nullable = propertyTypeInfo.IsGenericType && propertyTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
                var baseType = nullable ? Nullable.GetUnderlyingType(property.PropertyType) : property.PropertyType;
                var baseTypeInfo = baseType.GetTypeInfo();
                if (baseTypeInfo.IsEnum)
                {
                    baseType = baseTypeInfo.GetEnumUnderlyingType();
                }

                if (nullableAttribute != null)
                {
                    // Override if its a string, otherwise require consistency
                    if (baseType == typeof(string))
                    {
                        nullable = true;
                    }
                    else if (!nullable)
                    {
                        throw new InvalidOperationException("Property with SqlNullable attribute must be nullable");
                    }
                }

                string propertyName;
                var attr = property.GetCustomAttribute<SqlFieldAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.Name))
                {
                    propertyName = attr.Name;
                }
                else
                {
                    propertyName = property.Name;
                }

                var decimalPrecision = 0;
                var decimalScale = 0;
                if (baseType == typeof(decimal))
                {
                    if (decimalAttribute != null)
                    {
                        decimalPrecision = decimalAttribute.Precision;
                        decimalScale = decimalAttribute.Scale;
                    }
                    else
                    {
                        decimalPrecision = 13;
                        decimalScale = 5;
                    }
                }

                Columns.Add(new Column()
                {
                    MemberName = property.Name,
                    SqlName = propertyName,
                    PrimaryKey = primaryKeyAttribute != null,
                    PrimaryKeyAutoIncrement = primaryKeyAttribute?.AutoIncrement ?? false,
                    OriginalType = property.PropertyType,
                    BaseType = baseType,
                    Nullable = nullable,
                    PropertyInfo = property,
                    SqlType = new SqlTypeInfo()
                    {
                        StringLength = stringAttribute?.Length ?? 0,
                        StringNVarChar = stringAttribute?.NVarChar ?? false,
                        DecimalPrecision = decimalPrecision,
                        DecimalScale = decimalScale,
                    }
                });
            }

            var foreignKeyAttributes = typeInfo.GetCustomAttributes<ForeignKeyAttribute>();
            foreach (var foreignKeyAttribute in foreignKeyAttributes)
            {
                if (string.IsNullOrEmpty(foreignKeyAttribute.Name))
                {
                    throw new InvalidOperationException("ForeignKey attribute must specify a name");
                }

                ForeignKeys.Add(new ForeignKey()
                {
                    Name = foreignKeyAttribute.Name,
                    Columns = foreignKeyAttribute.Columns.ToList() ?? new List<string>(),
                    ReferenceTableType = foreignKeyAttribute.ReferenceTableType,
                    ReferenceColumns = foreignKeyAttribute.ReferenceColumns.ToList() ?? new List<string>()
                });
            }

            var indexAttributes = typeInfo.GetCustomAttributes<IndexAttribute>();
            foreach (var indexAttribute in indexAttributes)
            {
                if (string.IsNullOrEmpty(indexAttribute.Name))
                {
                    throw new InvalidOperationException("Index attribute must specify a name");
                }

                Indices.Add(new Index()
                {
                    Name = indexAttribute.Name,
                    Columns = indexAttribute.Columns.ToList() ?? new List<string>(),
                    Unique = indexAttribute.Unique,
                });
            }
        }
    }
}
