using System;

namespace TypedSql.Test
{
    public class Product
    {
        [PrimaryKey(AutoIncrement = true)]
        public int ProductId { get; set; }
        public string Name { get; set; }
    }

    [ForeignKey(Name = "fk_unit_product",
        Columns = new[] { nameof(ProductId) },
        ReferenceColumns = new[] { nameof(Product.ProductId) },
        ReferenceTableType = typeof(Product))
    ]
    public class Unit
    {
        [PrimaryKey(AutoIncrement = true)]
        public int UnitId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
    }

    [SqlTable(Name = "inventory_db")]
    [ForeignKey(Name = "fk_inventory_unit",
        Columns = new[] { nameof(UnitId) }, 
        ReferenceColumns = new[] { nameof(Unit.UnitId) }, 
        ReferenceTableType = typeof(Unit))
    ]
    public class Inventory
    {
        [PrimaryKey(AutoIncrement = true)]
        [SqlField(Name = "inventory_id")]
        public int InventoryId { get; set; }

        [SqlField(Name = "unit_id")]
        public int UnitId { get; set; }

        [SqlField(Name = "stock")]
        public int Stock { get; set; }
    }

    public enum IntEnumType : int
    {
        TestValue0 = 0,
        TestValue1 = 1,
        TestValue2 = 2,
        TestValue3 = 3,
    }

    public class TypeValue
    {
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public short ShortValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public long LongValue { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public string StringValue { get; set; }
        public IntEnumType IntEnumValue { get; set; }
        public byte[] BlobValue { get; set; }

        // Not supported on SQL Server:
        // public sbyte SbyteValue { get; set; }
        // public ushort UshortValue { get; set; }
        // public uint UintValue { get; set; }
        // public ulong UlongValue { get; set; }
    }

    public class AttributeValue
    {
        [SqlString(Length = 100, NVarChar = true)]
        public string Length100Unicode { get; set; }

        [SqlDecimal(Precision = 10, Scale = 7)]
        public decimal DecimalPrecision { get; set; }
    }

    public class TestDataContext : DatabaseContext
    {
        public FromQuery<Product> Products { get; set; }
        public FromQuery<Unit> Units { get; set; }
        public FromQuery<Inventory> Inventories { get; set; }
        public FromQuery<TypeValue> TypeValues { get; set; }
        public FromQuery<AttributeValue> AttributeValues { get; set; }
    }
}
