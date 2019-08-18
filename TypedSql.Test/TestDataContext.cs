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

    public class TestDataContext : DatabaseContext
    {
        public FromQuery<Product> Products { get; set; }
        public FromQuery<Unit> Units { get; set; }
    }
}
