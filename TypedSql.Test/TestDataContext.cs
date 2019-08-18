namespace TypedSql.Test
{
    public class Product
    {
        [PrimaryKey(AutoIncrement = true)]
        public int ProductId { get; set; }
        public string Name { get; set; }
    }

    public class Unit
    {
        [PrimaryKey(AutoIncrement = true)]
        public int UnitId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
    }

    [SqlTable(Name = "inventory_db")]
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

    public class TestDataContext : DatabaseContext
    {
        public FromQuery<Product> Products { get; set; }
        public FromQuery<Unit> Units { get; set; }
        public FromQuery<Inventory> Inventories { get; set; }
    }
}
