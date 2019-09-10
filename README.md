# TypedSql

**EXPERIMENTAL** Write database queries in C# and syntax as close to real SQL as possible.

## About

The primary focus of TypedSql is to write readable and maintainable SQL queries. Object-relational mapping is generally left to the user. TypedSql is inspired by and somewhat similar to Entity Framework and Linq2Sql, but by design there is:

- No change tracking => scales better
- No navigation properties => explicit joins
- No client evaluation => fewer surprises
- No Linq => leaner abstraction

## Features

- SELECT, INSERT, UPDATE, DELETE
- INNER JOIN, LEFT JOIN
- GROUP BY, HAVING
- ORDER BY, LIMIT, OFFSET
- CREATE TABLE, DROP TABLE
- DECLARE, SET SQL variables
- Aggregate SQL functions AVERAGE(), COUNT(), SUM()
- Scalar SQL functions YEAR(), MONTH(), DAY(), HOUR(), MINUTE(), SECOND(), LAST_INSERT_ID()
- Batch multiple SQL statements
- Composable SQL subqueries
- Implementations for SQL Server, MySQL and in-memory

## Getting the binaries

For now you need to create a `nuget.config` file in the root of your solution pointing at the build servers Nuget feed:

```xml
<configuration>
    <packageSources>
        <add key="TypedSql AppVeyor Feed" value="https://ci.appveyor.com/nuget/typedsql-wo2kmq2wc3dg" />
    </packageSources>
</configuration>
```

Reload the solution and the packages `TypedSql`, `TypedSql.SqlServer` and `TypedSql.MySql` should be available in your Nuget package manager.

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

var query = stmtList.Select(db.Products.Where(p => p.ProductId == 1));

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
        ));
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
        ));
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
        .GroupBy(
            a => new { a.ProductId },
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

Translated to SQL:

```sql
INSERT INTO Product (Name)
SELECT CONCAT("Product from ", a.Name) AS Name
FROM Unit a
```

### UPDATE

```c#
stmtList.Update(
    DB.Products
        .Where(p => p.ProductId == 1),
    (p, builder) => builder
        .Value(b => b.Name, p + ": Not tonight"));
```

Translated to SQL:

```sql
UPDATE Product
SET Name = CONCAT(Name, ": Not tonight")
WHERE ProductId = 1
```

## Basic usage with SQL Server

Add a dependency on the `TypedSql.SqlServer` package.

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

Add a dependency on the `TypedSql.MySql` package.

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

The in-memory runner is included in the `TypedSql` package.

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

## SQL types

|.NET Type|SQL Type|
|-|-|
|`bool`|`BIT`|
|`byte`|`TINYINT` in SQL Server<br>`TINYINT UNSIGNED` in MySQL|
|`sbyte`|Throws in SQL Server<br>`TINYINT` in MySQL|
|`short`|`SMALLINT`|
|`ushort`|Throws in SQL Server<br>`SMALLINT UNSIGNED` in MySQL|
|`int`|`INT`|
|`uint`|Throws in SQL Server<br>`INT UNSIGNED` in MySQL|
|`long`|`BIGINT`|
|`decimal`|`DECIMAL(13, 5)`|
|`float`|`REAL`|
|`double`|`REAL`|
|`string`|`NVARCHAR(MAX)` in SQL Server<br>`VARCHAR(1024)` in MySQL|
|`DateTime`|`DATETIME2` in SQL Server<br>`DATETIME` in MySQL|

## SQL functions and operators

TypedSql supports SQL functions and operators through a static `Function` class with the following methods:

|.NET Method|SQL Equivalent|
|-|-|
|`Function.Count(ctx, selector)`|`COUNT()`|
|`Function.Sum(ctx, selector)`|`SUM()`|
|`Function.Average(ctx, selector)`|`AVG()`|
|`Function.Like(lhs, rhs)`|`lhs LIKE rhs`|
|`Function.Contains(ctx, value, subquery)`|`value IN (SELECT ...)`|
|`Function.Contains(value, enumerable)`|`value IN (...)`|
|`Function.LastInsertIdentity(ctx)`|`SCOPE_IDENTITY` in SQL Server<br>`LAST_INSERT_ID` in MySQL|
|`Function.Hour(dateTime)`|`HOUR()`|
|`Function.Minute(dateTime)`|`MINUTE()`|
|`Function.Second(dateTime)`|`SECOND()`|
|`Function.Year(dateTime)`|`YEAR()`|
|`Function.Month(dateTime)`|`MONTH()`|
|`Function.Day(dateTime)`|`DAY()`|

