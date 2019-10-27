# TypedSql

**EXPERIMENTAL** Write database queries in C# and syntax as close to real SQL as possible.

[![Build status](https://ci.appveyor.com/api/projects/status/luv9d8m96u2nweqk?svg=true)](https://ci.appveyor.com/project/andersnm/typedsql)
[![Code coverage](https://codecov.io/gh/andersnm/TypedSql/branch/master/graph/badge.svg)](https://codecov.io/gh/andersnm/TypedSql)

## About

The primary focus of TypedSql is to write readable and maintainable SQL queries. Object-relational mapping is generally left to the user, although TypedSql is capable of returning complex object hierarchies without arrays.
TypedSql is inspired by and somewhat similar to Entity Framework and Linq2Sql, but by design there is:

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
- Aggregate SQL functions AVERAGE(), COUNT(), SUM(), MIN(), MAX()
- Scalar SQL functions YEAR(), MONTH(), DAY(), HOUR(), MINUTE(), SECOND(), LAST_INSERT_ID()
- Batch multiple SQL statements
- Composable SQL subqueries
- Implementations for SQL Server, MySQL and in-memory
- Migrations

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

### Basic example: SELECT ... WHERE ...

Query in C#:

```c#

var runner = new InMemoryQueryRunner();
var db = new TestDataContext();
var stmtList = new StatementList();
var query = stmtList.Select(db.Products.Where(p => p.ProductId == 1));

foreach (var row in runner.ExecuteQuery(query)) {
    Console.WriteLine("{0}: {1}", row.ProductId, row.Name);
}

```

Translated to SQL:

```sql
SELECT a.ProductId, a.ProductName
FROM Product a
WHERE a.ProductId = 1
```

### SELECT ... INNER JOIN [table]

A table can be specified in the first parameter to `Join()`, which generates SQL with a plain join:

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

### SELECT ... INNER JOIN [subquery]

Any query that is not a table object can be specified in the first parameter to `Join()`, which generates SQL with a joined subquery:

```c#
var query = stmtList.Select(
    db.Products
        .Where(p => p.ProductId == 1)
        .Join(
            Db.Units.Project((ctx, u) => new { u.ProductId, u.Name }),
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
INNER JOIN (SELECT b.ProductId, b.Name FROM Unit b) c ON a.ProductId = c.ProductId
WHERE a.ProductId = 1
```


### SELECT ... LEFT JOIN

The joined side in a LEFT JOIN can be null, so field accesses in the query code must be null-checked.
The SQL generator recognizes null-checking shorthand patterns for nullables, and generates SQL without any actual null checks, since this is handled transparently in the SQL language:

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

Insert (and update) statements use the `InsertBuilder` class to assign to SQL fields in a typed way:

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

Update (and insert) statements use the `InsertBuilder` class to assign to SQL fields in a typed way:

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

### SELECT ... FROM (SELECT ...)

Use the `Select()` method to wrap a query in a subquery:

```c#
stmtList.Select(DB.Products.Select((ctx, p) => p));
```

Translated to SQL:

```sql
SELECT a.ProductId, a.Name
FROM (SELECT b.ProductId, b.Name FROM Product b) a
```

### SELECT (SELECT ...) FROM ...

Use the `AsExpression()` method to treat a query as an expression:

```c#
stmtList.Select(
    DB.Products.Project((ctx, p) => new {
        p.ProductId,
        SomeUnitId = DB.Units.Limit(1).Project(u => u.UnitId).AsExpression(ctx),
    })
);
```

Translated to SQL:

```sql
SELECT a.ProductId, (SELECT b.UnitId FROM Unit b LIMIT 1) SomeUnitId
FROM Product a
```

## Important classes

### The DatabaseContext class

`DatabaseContext` is the base class for a database schema. Any members having type `FromQuery` in derived classes are automatically instantiated by the constructor. This class is independent of the database connection.

### The SelectorContext class

Most query expression take a parameter of type `SelectorContext` or `SelectorContext<T>` for keeping track of intermediate state during in-memory evaluation.
The context is a required parameter in many `Function.*` helper methods like `Sum` or `Average`.

### The InsertBuilder class

The `InsertBuilder` class is used in insert and update statements to assign to SQL fields in a typed way.

Use the `Value()` method to assign a value to field. The syntax is a bit unusual, f.ex the following assigns a constant string to the ProductName property of a table type:

```c#
builder.Value(p => p.ProductName, "New name")` 
```

Use the `Values()` method to copy fields and values to set from another InsertBuilder instance. F.ex to selectively update/insert specific fields:

```c#
var productId = /* ... */
var productName = /* ... */

var builder = new InsertBuilder<Product>();
builder.Value(p => p.UpdateDate, DateTime.Now);

// Only update if specified
if (productName != null)
{
    builder.Value(p => p.ProductName, "New name")` 
}

runner.Update(
    db.Products.Where(p => p.ProductId == productId),
    (p, insert) => insert.Values(builder));
```

### The StatementList class

The `StatementList` class defines a batch of SQL statements to send to the database server.

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

## Default SQL types

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

## SQL type modifier attributes

Properties may be decorated with attributes to specify the default types:

```c#
public class Example {
    // NVARCHAR(100) on SqlServer
    // VARCHAR(100) on MySql
    [SqlString(Length = 100, NVarChar = true)]
    public string Length100Unicode { get; set; }

    // DECIMAL(10,7)
    [SqlDecimal(Precision = 10, Scale = 7)]
    public decimal DecimalPrecision { get; set; }
};
```

## SQL functions and operators

TypedSql supports SQL functions and operators through a static `Function` class with the following methods:

|.NET Method|SQL Equivalent|
|-|-|
|`Function.Count(ctx, selector)`|`COUNT()`|
|`Function.Sum(ctx, selector)`|`SUM()`|
|`Function.Average(ctx, selector)`|`AVG()`|
|`Function.Min(ctx, selector)`|`MIN()`|
|`Function.Max(ctx, selector)`|`MAX()`|
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

## Migrations

### Migration tool

The migration tool requires .NET Core 3.0 SDK or newer, and is installed as a local tool in the database project directory.

The migration tool is a simple code generator and implements a single "add-migration" command which does the following:

* Load the database project assembly
* Scan for a class inheriting from DatabaseContext and existing migration classes
* Generate a migration class with the current database state, and Up()/Down() methods migrating the database from the previous to the current state

There are limitations what kind of assemblies can be loaded dynamically by the tool. 
The tool supports any .NET standard class library and most netcoreapp3.0 application assemblies. Only application assemblies referencing version 3.0 of the shared frameworks Microsoft.NETCore.App, Microsoft.AspNetCore.App and/or Microsoft.WindowsDesktop.App are supported.

This means f.ex when developing for ASP.NET Core 2.x and want to use TypedSql migrations, the DatabaseContext class should reside in a separate class library outside of the web project.

Some times during development, users might want to unapply and remove a migration before generating a new migration with improvements. In these cases, please note the following:

- The tool does not support to connect to a database and apply/unapply migrations. This is left to the user to implement.
- The tool does not support to remove generated migration classes. Instead the user should delete the generated files.
- Remember to build the database project after deleting a migration, before generating a new migration. Some times a full rebuild might be required for the build tools to detect deleted files.

Install and use the tool in a shell from the database project directory:

```bash
# Run this once if you haven't installed any local tools yet
dotnet new tool-manifest

# Run this once to install the TypedSql CLI tool
dotnet tool install TypedSql.CliTool

# Run this later to update the TypedSql CLI tool
dotnet tool update TypedSql.CliTool

# Show available commands
dotnet typedsql --help
dotnet typedsql add-migration --help

# Generate a new migration class named "Initial" in ./Migrations
dotnet typedsql add-migration -a ./path/to/your/assembly.dll -n Initial
```

### Applying migrations

The application can apply migrations using the `TypedSql.Migrator` class:

```c#
SqlQueryRunner runner = /* ... */
var migrator = new Migrator();
migrator.ReadAssemblyMigrations(typeof(MyDatabaseContext).Assembly);
migrator.ReadAppliedMigrations(runner);
migrator.MigrateToLatest(runner);
```
