using System;
using System.Linq;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace TypedSql.Test
{
    [TestFixture]
    public class SelectTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConstants(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(ctx => new { X = 1, Y = true, Z = "test" });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "X should be true");
            Assert.AreEqual("test", results[0].Z, "X should be 'test'");
        }

        class TestClass
        {
            public int X { get; set; }
            public bool Y { get; set; }
            public string Z { get; set; }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConstantsIntoClass(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(ctx => new TestClass() { X = 1, Y = true, Z = "test" });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "X should be true");
            Assert.AreEqual("test", results[0].Z, "X should be 'test'");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectTableIntoClass(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products.Where(p => p.ProductId == 1),
                (ctx, a) => new TestClass() {
                    X = a.ProductId,
                    Y = true,
                    Z = a.Name
                });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "Y should be true");
            Assert.AreEqual("Happy T-Shirt", results[0].Z, "Z should be product name");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectTable(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .Join(DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new
                        {
                            Product = a,
                            Unit = b,
                        })
                    .Where(p => p.Unit.UnitId == 1)
                    .Project((ctx, p) => p.Product),
                (ctx, a) => a);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectRenamedTableWithRenamedField(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Inventories,
                (ctx, a) => a);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(2, results.Count, "Should be 2 result");
            Assert.AreEqual(1, results[0].InventoryId, "InventoryId should be 1");
            Assert.AreEqual(1, results[0].UnitId, "UnitId should be 1");
            Assert.AreEqual(10, results[0].Stock, "Stock should be 10");
        }


        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinTable(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .Join(
                        DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            ProductName = a.Name,
                            b.UnitId,
                            UnitName = b.Name,
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinTableObject(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .Join(
                        DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new { Product = a, Unit = b, X = 1234 }),
                (ctx, p) => new {
                    p.Product.ProductId,
                    p.Product.Name,
                    UnitName = p.Unit.Name,
                    p.X,
                });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectLeftJoinTable(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            ProductName = a.Name,
                            UnitId = b != null ? (int?)b.UnitId : null, // HMMMMM
                            UnitName = b != null ? b.Name : null,
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            // var sql = runner.GetSql(stmtList, out _);
            Assert.True(results.Count > 0, "Should be results");
            foreach (var row in results)
            {
                if (row.ProductId == 1)
                {
                    Assert.IsNotNull(row.UnitId, "UnitId not null");
                    Assert.IsNotNull(row.UnitName, "UnitName not null");
                }
                else if (row.ProductId == 2)
                {
                    Assert.IsNull(row.UnitId, "UnitId null");
                    Assert.IsNull(row.UnitName, "UnitName null");
                }
            }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectNullProp(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            ProductName = a.Name,
                            UnitId = b != null ? (int?)b.UnitId : null,
                            UnitName = b != null ? b.Name : null,
                        })
                    .Project(
                        (ctx, p) => new {
                            UnitId = p.UnitId != null ? (int)p.UnitId : 0,
                            UnitId2 = p.UnitId ?? 0
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinSubquery(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var subquery = DB.Units.GroupBy(u => new { u.ProductId }, (ctx, ur) => new { ur.ProductId, UnitCount = Function.Count(ctx, t => t.UnitId) });

            var select = stmtList.Select(
                DB.Products
                    .Join(
                        subquery,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            ProductName = a.Name,
                            b.UnitCount
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinSubqueryNames(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var select = stmtList.Select(
                DB.Units
                    .Join(
                        DB.Inventories.Where(i => i.Stock > 0),
                        (actx, a, bctx, b) => a.UnitId == b.UnitId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            UnitName = a.Name,
                            b.UnitId
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectCountSubquery(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var subquery = DB.Units.Select((ctx, ur) => ur);

            var select = stmtList.Select(
                subquery,
                (ctx, p) => new { Count = Function.Count(ctx, x => x.UnitId )});

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be results");
            Assert.AreEqual(3, results[0].Count, "Should count units");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        // [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereNull(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(DB.Products.Where(p => p.Name == null), (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var sql = runner.GetSql(stmtList, out _);
            Assert.True(sql.IndexOf(" IS NULL") != -1, "SQL should contain the string 'IS NULL': " + sql);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(0, results.Count, "Should be no results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereContains(Type runnerType)
        {
            var productIds = new[] { 1, 200, 300 };
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(
                DB.Products
                    .Where(
                        p => Function.Contains(p.ProductId, productIds)),
                        (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test, Ignore("TODO: Function.Contains with inline arrays")]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereContainsInline(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(DB.Products.Where(p => Function.Contains(p.ProductId, new[] { 1, 200, 300 })), (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConcatExpression(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var select = stmtList.Select(DB.Products, (ctx, p) => new { Name = "Product: " + p.Name });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.True(results.Count > 0, "Should be results");

            foreach (var result in results)
            {
                Assert.True(result.Name.StartsWith("Product: "), "Name should start with 'Product: ' " + result.Name);
            }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectPlaceholder(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var testvar = stmtList.DeclareSqlVariable<int>("testvar");
            stmtList.SetSqlVariable(testvar, ctx => 1234);

            var select = stmtList.Select(DB.Products, (ctx, p) => new { VariableValue = testvar.Value });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.True(results.Count > 0, "Should be results");

            foreach (var result in results)
            {
                Assert.AreEqual(1234, result.VariableValue, "Selected VariableValue should be 1234");
            }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWherePlaceholder(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var testvar = stmtList.DeclareSqlVariable<int>("testvar");
            stmtList.SetSqlVariable(testvar, ctx => 1);

            var select = stmtList.Select(DB.Products.Where(p => p.ProductId == testvar.Value), (ctx, p) => new { p.ProductId });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 result");

            foreach (var result in results)
            {
                Assert.AreEqual(1, result.ProductId, "Selected product id should be 1");
            }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectOrderByDesc(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var select = stmtList.Select(DB.Products.OrderBy(false, p => p.Name), (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual("Test Product Without Units", results[0].Name, "Test Product Without Units");
            Assert.AreEqual("Happy T-Shirt", results[1].Name, "Happy T-Shirt");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectOrderByAsc(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var select = stmtList.Select(DB.Products.OrderBy(true, p => p.Name), (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual("Happy T-Shirt", results[0].Name, "Happy T-Shirt");
            Assert.AreEqual("Test Product Without Units", results[1].Name, "Test Product Without Units");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectGroupByCount(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (qctx, q, wctx, w) => q.ProductId == w.ProductId,
                        (qctx, q, wctx, w) => new { Product = q, Unit = w })
                    .GroupBy(
                        u => new { u.Product.ProductId }, 
                        (ctx, ur) => new {
                            ur.Product.ProductId,
                            UnitCount = Function.Count(ctx, t => t.Unit.UnitId)
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(2, results.Count, "Should be 2 results");
            Assert.AreEqual(1, results[0].ProductId, "Row 0: ProductId == 1");
            Assert.AreEqual(3, results[0].UnitCount, "Row 0: UnitCount == 3");

            Assert.AreEqual(2, results[1].ProductId, "Row 1: ProductId == 2");
            Assert.AreEqual(1, results[1].UnitCount, "Row 1: UnitCount == 1");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectGroupBySum(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (qctx, q, wctx, w) => q.ProductId == w.ProductId,
                        (qctx, q, wctx, w) => new { Product = q, Unit = w })
                    .GroupBy(
                        u => new { u.Product.ProductId },
                        (ctx, ur) => new {
                            ur.Product.ProductId,
                            PriceSum = Function.Sum(ctx, t => t.Unit != null ? (int?)t.Unit.Price : null)
                        }),
                (ctx, p) => p);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(2, results.Count, "Should be 2 results");
            Assert.AreEqual(1, results[0].ProductId, "Row 0: ProductId == 1");
            Assert.AreEqual(300, results[0].PriceSum, "Row 0: PriceSum == 300");

            Assert.AreEqual(2, results[1].ProductId, "Row 1: ProductId == 2");
            Assert.AreEqual(null, results[1].PriceSum, "Row 1: PriceSum == null");
        }
    }
}
