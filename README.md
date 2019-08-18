# TypedSql

**EXPERIMENTAL** Write database queries in C# and syntax as close to real SQL as possible.

## Features

- SELECT, INSERT, UPDATE, DELETE
- INNER JOIN, LEFT JOIN
- GROUP BY, HAVING and aggregate COUNT, SUM SQL functions
- ORDER BY, LIMIT, OFFSET
- SQL functions YEAR(), MONTH(), DAY(), HOUR(), MINUTE(), SECOND(), LAST_INSERT_ID()
- SQL variables
- Batch multiple SQL statements
- Composable SQL subqueries
- Implementations for SQL Server, MySQL and in-memory

## Examples

The examples are based on the following data context definition:

```c#
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
}

public class TestDataContext : DatabaseContext
{
    public FromQuery<Product> Products { get; set; }
    public FromQuery<Unit> Units { get; set; }
}
```

### SELECT ... FROM ...

Query in C#:

```c#

var db = new TestDataContext();
var stmtList = new SqlStatementList();

var query = stmtList.Select(
    db.Products
        .Where(p => p.ProductId == 1),
    (ctx, p) => p);

var enumerable = runner.ExecuteQuery(query);

```

Translated to SQL:

```sql
SELECT a.ProductId, a.ProductName
FROM Product a
WHERE a.ProductId = 1
```

### SELECT ... INNER JOIN

```c#
var query = stmtList.Select(
    db.Products
        .Where(p => p.ProductId == 1)
        .Join(
            Db.Units,
            (actx, a, bctx, b) => a.ProductId == b.ProductId,
            (actx, a, bctx, b) => new {
                a.ProductId,
                a.ProductName,
                b.UnitId,
                b.UnitName
            }
        ),
    (ctx, p) => p);
```

Translated to SQL:

```sql
SELECT a.ProductId, a.ProductName
FROM Product a
INNER JOIN Unit b ON a.ProductId = b.ProductId
WHERE a.ProductId = 1
```

### SELECT ... LEFT JOIN

```c#
var query = DataStatementList.Select(
    db.Products
        .Where(p => p.ProductId == 1)
        .LeftJoin(
            Db.Units,
            (actx, a, bctx, b) => a.ProductId == b.ProductId,
            (actx, a, bctx, b) => new {
                a.ProductId,
                a.ProductName,
                UnitId = b != null ? (int?)b.UnitId : null,
                UnitName = b.UnitName
            }
        ),
    (ctx, p) => p);
```

Translated to SQL:

```sql
SELECT a.ProductId, a.ProductName, b.UnitId, b.UnitName
FROM Product a
LEFT JOIN Unit b ON a.ProductId = b.ProductId
WHERE a.ProductId = 1
```

### SELECT ... GROUP BY

```c#
var query = DataStatementList.Select(
    db.Units
        .Where(p => p.ProductId == 1)
        .GroupBy(a => new { a.ProductId }, a => a),
    (ctx, p) => new {
        p.ProductId,
        UnitCount = Function.Count(ctx, u => u.UnitId)
    });
```

Translated to SQL:

```sql
SELECT a.ProductId, COUNT(a.UnitId) AS UnitCount
FROM Unit a
GROUP BY a.ProductId
WHERE a.ProductId = 1
```

### INSERT INTO ... VALUES ...

```c#
stmtList.Insert(
    DB.Products, insert =>
        insert.Value(p => p.Name, "Happy T-Shirt"));
```

Translated to SQL:

```sql
INSERT INTO Product (Name) VALUES ("Happy T-Shirt")
```

### INSERT INTO ... SELECT ...

```c#
stmtList.Insert(
    DB.Products,
    DB.Units,
    (x, insert) => insert
        .Value(p => p.Name, "Product from " + x.Name));
```

```sql
INSERT INTO Product (Name)
SELECT CONCAT("Product from ", a.Name) AS Name
FROM Unit a
```

## UPDATE

```c#
stmtList.Update(
    DB.Products
        .Where(p => p.ProductId == 1),
    (p, builder) => builder
        .Value(b => b.Name, p + ": Not tonight"));
```

```sql
UPDATE Product
SET Name = CONCAT(Name, ": Not tonight")
WHERE ProductId = 1
```

## Basic usage with SQL Server

```c#
using TypedSql;
using TypedSql.SqlServer;
// ...
var connection = new SqlConnection(connectionString);
var runner = new SqlServerQueryRunner(connection);
// ...
runner.ExecuteNonQuery(stmtList);
```

## Basic usage with MySQL

The MySQL connection string must include the statement `AllowUserVariables=true;`.

```c#
using TypedSql;
using TypedSql.MySql;
// ...
var connection = new MySqlConnection(connectionString);
var runner = new MySqlServerQueryRunner(connection);
// ...
runner.ExecuteNonQuery(stmtList);
```

## Basic in-memory usage

The data context is the data store when using the in-memory query runner, and therefore a singleton.

```c#
using TypedSql;
// ...
var runner = new InMemoryQueryRunner();
// ...
runner.ExecuteNonQuery(stmtList);
```

## Using with ASP.NET Core and MySQL

Register the connection and query runner as scoped. Register the data context as a singleton. In Startup.cs `ConfigureServices()`:

```c#
services.AddScoped(provider =>
{
    var connection = new MySqlConnection(Configuration["ConnectionString"]);
    connection.Open();
    return connection;
});

services.AddScoped<IQueryRunner>(provider =>
{
    return new MySqlQueryRunner(provider.GetRequiredService<MySqlConnection>());
});

services.AddSingleton<TestDataContext>();
```
