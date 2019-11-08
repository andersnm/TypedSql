using System;
using System.Collections.Generic;
using TypedSql;
using TypedSql.Migration;
using TypedSql.Schema;

public partial class Initial : IMigration
{
    public string Name => "201911081141_Initial";

    public List<SqlTable> Tables => new List<SqlTable>()
    {
        new SqlTable()
        {
            TableName = "Product",
            Columns = new List<SqlColumn>()
            {
                new SqlColumn()
                {
                    Name = "ProductId",
                    Type = typeof(Int32),
                    PrimaryKey = true,
                    PrimaryKeyAutoIncrement = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "Name",
                    Type = typeof(String),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
            },
            ForeignKeys = new List<SqlForeignKey>()
            {
            },
            Indices = new List<SqlIndex>()
            {
            },
        },
        new SqlTable()
        {
            TableName = "Unit",
            Columns = new List<SqlColumn>()
            {
                new SqlColumn()
                {
                    Name = "UnitId",
                    Type = typeof(Int32),
                    PrimaryKey = true,
                    PrimaryKeyAutoIncrement = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "ProductId",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "Name",
                    Type = typeof(String),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "Price",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
            },
            ForeignKeys = new List<SqlForeignKey>()
            {
                new SqlForeignKey()
                {
                    Name = "fk_unit_product",
                    ReferenceTableName = "Product",
                    Columns = new List<String>()
                    {
                        "ProductId",
                    },
                    ReferenceColumns = new List<String>()
                    {
                        "ProductId",
                    },
                },
            },
            Indices = new List<SqlIndex>()
            {
            },
        },
        new SqlTable()
        {
            TableName = "inventory_db",
            Columns = new List<SqlColumn>()
            {
                new SqlColumn()
                {
                    Name = "inventory_id",
                    Type = typeof(Int32),
                    PrimaryKey = true,
                    PrimaryKeyAutoIncrement = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "unit_id",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "stock",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
            },
            ForeignKeys = new List<SqlForeignKey>()
            {
                new SqlForeignKey()
                {
                    Name = "fk_inventory_unit",
                    ReferenceTableName = "Unit",
                    Columns = new List<String>()
                    {
                        "unit_id",
                    },
                    ReferenceColumns = new List<String>()
                    {
                        "UnitId",
                    },
                },
            },
            Indices = new List<SqlIndex>()
            {
            },
        },
        new SqlTable()
        {
            TableName = "TypeValue",
            Columns = new List<SqlColumn>()
            {
                new SqlColumn()
                {
                    Name = "BoolValue",
                    Type = typeof(Boolean),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "ByteValue",
                    Type = typeof(Byte),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "ShortValue",
                    Type = typeof(Int16),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "IntValue",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "NullableIntValue",
                    Type = typeof(Int32),
                    Nullable = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "FloatValue",
                    Type = typeof(Single),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "LongValue",
                    Type = typeof(Int64),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "DecimalValue",
                    Type = typeof(Decimal),
                    SqlType = new SqlTypeInfo()
                    {
                        DecimalPrecision = 13,
                        DecimalScale = 5,
                    },
                },
                new SqlColumn()
                {
                    Name = "DoubleValue",
                    Type = typeof(Double),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "DateTimeValue",
                    Type = typeof(DateTime),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "NullableDateTimeValue",
                    Type = typeof(DateTime),
                    Nullable = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "StringValue",
                    Type = typeof(String),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "NullableStringValue",
                    Type = typeof(String),
                    Nullable = true,
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "IntEnumValue",
                    Type = typeof(Int32),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
                new SqlColumn()
                {
                    Name = "BlobValue",
                    Type = typeof(Byte[]),
                    SqlType = new SqlTypeInfo()
                    {
                    },
                },
            },
            ForeignKeys = new List<SqlForeignKey>()
            {
            },
            Indices = new List<SqlIndex>()
            {
            },
        },
        new SqlTable()
        {
            TableName = "AttributeValue",
            Columns = new List<SqlColumn>()
            {
                new SqlColumn()
                {
                    Name = "Length100Unicode",
                    Type = typeof(String),
                    SqlType = new SqlTypeInfo()
                    {
                        StringLength = 100,
                        StringNVarChar = true,
                    },
                },
                new SqlColumn()
                {
                    Name = "DecimalPrecision",
                    Type = typeof(Decimal),
                    SqlType = new SqlTypeInfo()
                    {
                        DecimalPrecision = 10,
                        DecimalScale = 7,
                    },
                },
            },
            ForeignKeys = new List<SqlForeignKey>()
            {
            },
            Indices = new List<SqlIndex>()
            {
            },
        },
    };
}
