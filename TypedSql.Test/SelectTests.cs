using System;
using System.Linq;
using System.Linq.Expressions;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using TypedSql.PostgreSql;
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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConstants(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var n7 = 7;
            var n2 = 2;
            var n22 = 22;
            var n11 = 11;
            var results = runner.Select(ctx => new {
                X = 1,
                Y = true,
                Z = "test",
                DateValue = DateTime.Now, // also testing static property lookup
                DecimalValue = 4.5M,
                DoubleValue = 1.23D,
                FloatValue = 6.4f,
                MultiplyValue = 3 * n7,
                DivideValue = 42 / n2,
                ModulusValue = 483 % n22,
                AddValue = 10 + n11,
                SubtractValue = 32 - n11,
            }).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "X should be true");
            Assert.AreEqual("test", results[0].Z, "X should be 'test'");
            Assert.AreEqual(4.5M, results[0].DecimalValue, "DecimalValue should be 4.5");
            Assert.AreEqual(1.23D, results[0].DoubleValue, "DoubleValue should be 1.23");
            Assert.AreEqual(6.4f, results[0].FloatValue, "FloatValue should be 6.4");
            Assert.AreEqual(21, results[0].MultiplyValue, "Multiply 21");
            Assert.AreEqual(21, results[0].DivideValue, "Divide 21");
            Assert.AreEqual(21, results[0].ModulusValue, "Modulus 21");
            Assert.AreEqual(21, results[0].AddValue, "Add 21");
            Assert.AreEqual(21, results[0].SubtractValue, "Subtract 21");
        }

        class TestClass
        {
            public int X { get; set; }
            public bool Y { get; set; }
            public string Z { get; set; }
            public int UnusedMember { get; set; }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConstantsIntoClass(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(ctx => new TestClass() { X = 1, Y = true, Z = "test" }).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "X should be true");
            Assert.AreEqual("test", results[0].Z, "X should be 'test'");
            Assert.AreEqual(default(int), results[0].UnusedMember, "Unused member should have default value");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectTableIntoClass(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(
                DB.Products
                    .Where(p => p.ProductId == 1)
                    .Project((ctx, a) => new TestClass()
                    {
                        X = a.ProductId,
                        Y = true,
                        Z = a.Name
                    })).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].X, "X should be 1");
            Assert.AreEqual(true, results[0].Y, "Y should be true");
            Assert.AreEqual("Happy T-Shirt", results[0].Z, "Z should be product name");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectTable(Type runnerType)
        {
            var stmtList = new StatementList();
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
                    .Project((ctx, p) => p.Product));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectScalar(Type runnerType)
        {
            var stmtList = new StatementList();
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
                    .Project((ctx, p) => p.Product.Name + ", " + p.Unit.Name));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual("Happy T-Shirt, Happy XL", results[0], "Selected 'Happy T-Shirt, Happy XL'");
        }
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectRenamedTableWithRenamedField(Type runnerType)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(DB.Inventories);

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(3, results.Count, "Should be 3 result");
            Assert.AreEqual(1, results[0].InventoryId, "InventoryId should be 1");
            Assert.AreEqual(1, results[0].UnitId, "UnitId should be 1");
            Assert.AreEqual(10, results[0].Stock, "Stock should be 10");
        }


        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinTable(Type runnerType)
        {
            var stmtList = new StatementList();
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
                        }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinTableObject(Type runnerType)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(
                DB.Products
                    .Join(
                        DB.Units,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new { Product = a, Unit = b, X = 1234 })
                    .Project((ctx, p) => new {
                        p.Product.ProductId,
                        p.Product.Name,
                        UnitName = p.Unit.Name,
                        p.X,
                    }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectComplexObject(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(ctx =>
                new
                {
                    StringValue = "Test",
                    ObjectValue = new
                    {
                        InnerString = "InnerString",
                        InnerObject = new
                        {
                            InnestString = "InnestString",
                            IsSuccess = true,
                        },
                        NullObject = new
                        {
                            Nullable1 = (int?)null,
                            Nullable2 = (decimal?)null,
                        },
                        InnerInt = 4321,
                        TestObject = new TestClass()
                        {
                            X = 123,
                        },
                    },
                    IntValue = 1234,
                }).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual("Test", results[0].StringValue, "Should be Test");
            Assert.AreEqual(1234, results[0].IntValue, "Should be 1234");
            Assert.AreEqual("InnerString", results[0].ObjectValue.InnerString, "Should be InnerString");
            Assert.AreEqual(4321, results[0].ObjectValue.InnerInt, "Should be 4321");

            // TODO?? normalize behaviour
            if (runner is InMemoryQueryRunner)
            {
                Assert.AreNotEqual(null, results[0].ObjectValue.NullObject, "Should be null from DBs, but non-null in-memory");
            }
            else
            {
                Assert.AreEqual(null, results[0].ObjectValue.NullObject, "Should be null from DBs, but non-null in-memory");
            }
            Assert.AreEqual("InnestString", results[0].ObjectValue.InnerObject.InnestString, "Should be InnestString");
            Assert.AreEqual(true, results[0].ObjectValue.InnerObject.IsSuccess, "Should be true");
            Assert.AreEqual(123, results[0].ObjectValue.TestObject.X, "Should be 123");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectLeftJoinTable(Type runnerType)
        {
            var stmtList = new StatementList();
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
                        }));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectNullProp(Type runnerType)
        {
            var stmtList = new StatementList();
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
                        }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinSubquery(Type runnerType)
        {
            var stmtList = new StatementList();

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
                        }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectJoinSubqueryNames(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(
                DB.Units
                    .Join(
                        DB.Inventories.Where(i => i.Stock > 0),
                        (actx, a, bctx, b) => a.UnitId == b.UnitId,
                        (actx, a, bctx, b) => new {
                            a.ProductId,
                            UnitName = a.Name,
                            b.UnitId
                        }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.True(results.Count > 0, "Should be results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectCountSubquery(Type runnerType)
        {
            var stmtList = new StatementList();
            var subquery = DB.Units.Select((ctx, ur) => ur);

            var select = stmtList.Select(
                subquery.Project((ctx, p) => new { Count = Function.Count(ctx, x => x.UnitId )}));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be results");
            Assert.AreEqual(3, results[0].Count, "Should count units");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        // [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereNull(Type runnerType)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(DB.Products.Where(p => p.Name == null));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereAndOrPrecedence(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(DB.TypeValues.Where(t => (t.IntEnumValue == IntEnumType.TestValue1 || t.IntEnumValue == IntEnumType.TestValue2) && t.NullableDateTimeValue != null && t.NullableDateTimeValue < DateTime.Now)).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereContains(Type runnerType)
        {
            var productIds = new[] { 1, 200, 300 };
            var stmtList = new StatementList();
            var select = stmtList.Select(
                DB.Products
                    .Where(
                        p => Function.Contains(p.ProductId, productIds)));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test, Ignore("TODO: Function.Contains with inline arrays")]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereContainsInline(Type runnerType)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(DB.Products.Where(p => Function.Contains(p.ProductId, new[] { 1, 200, 300 })));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectConcatExpression(Type runnerType)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(DB.Products.Project((ctx, p) => new { Name = "Product: " + p.Name }));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectPlaceholder(Type runnerType)
        {
            var stmtList = new StatementList();
            var testvar = stmtList.DeclareSqlVariable<int>("testvar");
            stmtList.SetSqlVariable(testvar, ctx => 1234);

            var select = stmtList.Select(DB.Products.Project((ctx, p) => new { VariableValue = testvar.Value }));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWherePlaceholder(Type runnerType)
        {
            var stmtList = new StatementList();
            var testvar = stmtList.DeclareSqlVariable<int>("testvar");
            stmtList.SetSqlVariable(testvar, ctx => 1);

            var select = stmtList.Select(DB.Products.Where(p => p.ProductId == testvar.Value).Project((ctx, p) => new { p.ProductId }));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectOrderByDesc(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(DB.Products.OrderBy(builder => builder.Value(p => p.Name, false)));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual("Test Product Without Units", results[0].Name, "Test Product Without Units");
            Assert.AreEqual("Happy T-Shirt", results[1].Name, "Happy T-Shirt");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectOrderByAsc(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(DB.Products.OrderBy(builder => builder.Value(p => p.Name, true)));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual("Happy T-Shirt", results[0].Name, "Happy T-Shirt");
            Assert.AreEqual("Test Product Without Units", results[1].Name, "Test Product Without Units");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectGroupByCount(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (qctx, q, wctx, w) => q.ProductId == w.ProductId,
                        (qctx, q, wctx, w) => new { Product = q, Unit = w })
                    .OrderBy(builder => builder.Value(p => p.Product.ProductId, true))
                    .GroupBy(
                        u => new { u.Product.ProductId }, 
                        (ctx, ur) => new {
                            ur.Product.ProductId,
                            UnitCount = Function.Count(ctx, t => t.Unit.UnitId)
                        })
                    .Having(p => p.UnitCount >= 1));

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
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectGroupBySum(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (qctx, q, wctx, w) => q.ProductId == w.ProductId,
                        (qctx, q, wctx, w) => new { Product = q, Unit = w })
                    .OrderBy(builder => builder.Value(p => p.Product.ProductId, true))
                    .GroupBy(
                        u => new { u.Product.ProductId },
                        (ctx, ur) => new {
                            ur.Product.ProductId,
                            PriceSum = Function.Sum(ctx, t => t.Unit != null ? (int?)t.Unit.Price : null)
                        }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(2, results.Count, "Should be 2 results");
            Assert.AreEqual(1, results[0].ProductId, "Row 0: ProductId == 1");
            Assert.AreEqual(300, results[0].PriceSum, "Row 0: PriceSum == 300");

            Assert.AreEqual(2, results[1].ProductId, "Row 1: ProductId == 2");
            Assert.AreEqual(null, results[1].PriceSum, "Row 1: PriceSum == null");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectComplexResult(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(
                DB.Products
                    .LeftJoin(
                        DB.Units,
                        (qctx, q, wctx, w) => q.ProductId == w.ProductId,
                        (qctx, q, wctx, w) => new { Product = q, Unit = w }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            // Assert.AreEqual(2, results.Count, "Should be 2 results");
            Assert.AreEqual(1, results[0].Product.ProductId, "Row 0: ProductId == 1");
            Assert.AreEqual(1, results[0].Unit.UnitId, "Row 0: UnitId == 1");

            Assert.AreEqual(1, results[1].Product.ProductId, "Row 1: ProductId == 1");
            Assert.AreEqual(2, results[1].Unit.UnitId, "Row 1: UnitId == 2");

            Assert.AreEqual(1, results[2].Product.ProductId, "Row 2: ProductId == 1");
            Assert.AreEqual(3, results[2].Unit.UnitId, "Row 2: UnitId == 3");

            Assert.AreEqual(2, results[3].Product.ProductId, "Row 3: ProductId == 2");
            Assert.AreEqual(null, results[3].Unit, "Row 3: Unit == null");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereEnum(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(
                DB.TypeValues.Where(t => t.IntEnumValue == IntEnumType.TestValue1)).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 results");
            Assert.AreEqual(IntEnumType.TestValue1, results[0].IntEnumValue, "Should be TestValue1");
        }

        class Aggregated<T> where T : struct
        {
            public T? Sum { get; set; }
            public T? Average { get; set; }
            public int Count { get; set; }
            public T? Min { get; set; }
            public T? Max { get; set; }
        }

        void SelectAggregated<T>(Type runnerType, Expression<Func<SelectorContext<TypeValue>, TypeValue, Aggregated<T>>> func) where T : struct
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var date1 = new DateTime(2000, 1, 1, 14, 00, 00);
            var date2 = new DateTime(2000, 1, 1, 14, 00, 10);

            var results = runner.Select(DB.TypeValues.Project(func)).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            if (typeof(T) == typeof(DateTime))
            {
                Assert.AreEqual(date2, results[0].Max, "Max should be date2");
                Assert.AreEqual(date1, results[0].Min, "Min should be date1");
            }
            else
            {
                Assert.AreEqual(10, results[0].Max, "Max should be 10");
                Assert.AreEqual(1, results[0].Min, "Min should be 1");
                Assert.AreEqual(11, results[0].Sum, "Sum should be 11");
            }
            Assert.AreEqual(2, results[0].Count, "Count should be 2");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateByte(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<byte>()
            {
                Sum = Function.Sum(ctx, x => x.ByteValue),
                Count = Function.Count(ctx, x => x.ByteValue),
                Average = Function.Average(ctx, x => x.ByteValue),
                Min = Function.Min(ctx, x => x.ByteValue),
                Max = Function.Max(ctx, x => x.ByteValue),
            });
        }


        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateShort(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<short>()
            {
                Sum = Function.Sum(ctx, x => x.ShortValue),
                Count = Function.Count(ctx, x => x.ShortValue),
                Average = Function.Average(ctx, x => x.ShortValue),
                Min = Function.Min(ctx, x => x.ShortValue),
                Max = Function.Max(ctx, x => x.ShortValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateInt(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<int>()
            {
                Sum = Function.Sum(ctx, x => x.IntValue),
                Count = Function.Count(ctx, x => x.IntValue),
                Average = Function.Average(ctx, x => x.IntValue),
                Min = Function.Min(ctx, x => x.IntValue),
                Max = Function.Max(ctx, x => x.IntValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateNullableInt(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<int>()
            {
                Sum = Function.Sum(ctx, x => x.NullableIntValue),
                Count = Function.Count(ctx, x => x.NullableIntValue),
                Average = Function.Average(ctx, x => x.NullableIntValue),
                Min = Function.Min(ctx, x => x.NullableIntValue),
                Max = Function.Max(ctx, x => x.NullableIntValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateLong(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<long>()
            {
                Sum = Function.Sum(ctx, x => x.LongValue),
                Count = Function.Count(ctx, x => x.LongValue),
                Average = Function.Average(ctx, x => x.LongValue),
                Min = Function.Min(ctx, x => x.LongValue),
                Max = Function.Max(ctx, x => x.LongValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateDecimal(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<decimal>()
            {
                Sum = Function.Sum(ctx, x => x.DecimalValue),
                Count = Function.Count(ctx, x => x.DecimalValue),
                Average = Function.Average(ctx, x => x.DecimalValue),
                Min = Function.Min(ctx, x => x.DecimalValue),
                Max = Function.Max(ctx, x => x.DecimalValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateFloat(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<float>()
            {
                Sum = Function.Sum(ctx, x => x.FloatValue),
                Count = Function.Count(ctx, x => x.FloatValue),
                Average = Function.Average(ctx, x => x.FloatValue),
                Min = Function.Min(ctx, x => x.FloatValue),
                Max = Function.Max(ctx, x => x.FloatValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateDouble(Type runnerType)
        {
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<double>() {
                Sum = Function.Sum(ctx, x => x.DoubleValue),
                Count = Function.Count(ctx, x => x.DoubleValue),
                Average = Function.Average(ctx, x => x.DoubleValue),
                Min = Function.Min(ctx, x => x.DoubleValue),
                Max = Function.Max(ctx, x => x.DoubleValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateDateTime(Type runnerType)
        {
            var d = new DateTime(1970, 1, 1);
            SelectAggregated(runnerType, (ctx, t) => new Aggregated<DateTime>()
            {
                Sum = d,
                Count = Function.Count(ctx, x => x.DateTimeValue),
                Average = d, // TODO: new DateTime(1970, 1, 1),
                Min = Function.Min(ctx, x => x.DateTimeValue),
                Max = Function.Max(ctx, x => x.DateTimeValue),
            });
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectAggregateString(Type runnerType)
        {
            var stmtList = new StatementList();

            var select = stmtList.Select(
                DB.TypeValues.Project((ctx, t) => new
                {
                    Count = Function.Count(ctx, x => x.StringValue),
                    Min = Function.Min(ctx, x => x.StringValue),
                    Max = Function.Max(ctx, x => x.StringValue),
                }));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual("10", results[0].Max, "Max should be 10");
            Assert.AreEqual("1", results[0].Min, "Min should be 1");
            Assert.AreEqual(2, results[0].Count, "Count should be 2");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectPredicate(Type runnerType)
        {
            var stmtList = new StatementList();

            var orBuilder = new PredicateBuilder<Product>(x => x.ProductId == 1);
            orBuilder.OrElse(y => y.ProductId == 2);
            orBuilder.OrElse(z => z.ProductId == 3);

            var andBuilder = new PredicateBuilder<Product>(x => Function.Like(x.Name, "%"));
            andBuilder.AndAlso(orBuilder.GetPredicate());

            var select = stmtList.Select(
                DB.Products.Where(andBuilder.GetPredicate()));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results[0].ProductId);
            Assert.AreEqual(2, results[1].ProductId);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectBlob(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Select(DB.TypeValues).ToList();

            Assert.IsNotNull(results[0].BlobValue);
            Assert.AreEqual(200, results[0].BlobValue.Length, "Blob length should be 200");
            for (var i = 0; i < results[0].BlobValue.Length; i++) {
                Assert.AreEqual(i, results[0].BlobValue[i]);
            }
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereByteOperators(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var gt = runner.Select(DB.TypeValues.Where(t => t.ByteValue > 5).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, gt.Count);
            Assert.AreEqual(10, gt[0]);

            var gte = runner.Select(DB.TypeValues.Where(t => t.ByteValue >= 5).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, gte.Count);
            Assert.AreEqual(10, gte[0]);

            var lt = runner.Select(DB.TypeValues.Where(t => t.ByteValue < 5).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, lt.Count);
            Assert.AreEqual(1, lt[0]);

            var lte = runner.Select(DB.TypeValues.Where(t => t.ByteValue <= 5).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, lte.Count);
            Assert.AreEqual(1, lte[0]);

            var neq = runner.Select(DB.TypeValues.Where(t => t.ByteValue != 10).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, neq.Count);
            Assert.AreEqual(1, neq[0]);

            var not_eq = runner.Select(DB.TypeValues.Where(t => !(t.ByteValue == 10)).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, neq.Count);
            Assert.AreEqual(1, neq[0]);

            var negate_eq = runner.Select(DB.TypeValues.Where(t => -t.ByteValue == -10).Project((ctx, c) => c.ByteValue)).ToList();
            Assert.AreEqual(1, negate_eq.Count);
            Assert.AreEqual(10, negate_eq[0]);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectWhereNullableIntOperators(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var gt = runner.Select(DB.TypeValues.Where(t => t.NullableIntValue > 5).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, gt.Count);
            Assert.AreEqual(10, gt[0]);

            var gte = runner.Select(DB.TypeValues.Where(t => t.NullableIntValue >= 5).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, gte.Count);
            Assert.AreEqual(10, gte[0]);

            var lt = runner.Select(DB.TypeValues.Where(t => t.NullableIntValue < 5).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, lt.Count);
            Assert.AreEqual(1, lt[0]);

            var lte = runner.Select(DB.TypeValues.Where(t => t.NullableIntValue <= 5).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, lte.Count);
            Assert.AreEqual(1, lte[0]);

            var neq = runner.Select(DB.TypeValues.Where(t => t.NullableIntValue != 10).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, neq.Count);
            Assert.AreEqual(1, neq[0]);

            var not_eq = runner.Select(DB.TypeValues.Where(t => !(t.NullableIntValue == 10)).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, neq.Count);
            Assert.AreEqual(1, neq[0]);

            var negate_eq = runner.Select(DB.TypeValues.Where(t => -t.NullableIntValue == -10).Project((ctx, c) => c.NullableIntValue)).ToList();
            Assert.AreEqual(1, negate_eq.Count);
            Assert.AreEqual(10, negate_eq[0]);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectDateTimeFunctions(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var dates = runner.Select(DB.TypeValues.Where(t => t.ByteValue == 10).Project((ctx, c) => new {
                Year = Function.Year(c.DateTimeValue),
                Month = Function.Month(c.DateTimeValue),
                Day = Function.Day(c.DateTimeValue),
                Hour = Function.Hour(c.DateTimeValue),
                Minute = Function.Minute(c.DateTimeValue),
                Second = Function.Second(c.DateTimeValue),
                NullableYear = Function.Year(c.NullableDateTimeValue),
                NullableMonth = Function.Month(c.NullableDateTimeValue),
                NullableDay = Function.Day(c.NullableDateTimeValue),
                NullableHour = Function.Hour(c.NullableDateTimeValue),
                NullableMinute = Function.Minute(c.NullableDateTimeValue),
                NullableSecond = Function.Second(c.NullableDateTimeValue),
            })).ToList();

            // date seed = new DateTime(2000, 1, 1, 14, 00, 10);
            Assert.AreEqual(1, dates.Count, "Expected 1 result");
            Assert.AreEqual(2000, dates[0].Year);
            Assert.AreEqual(1, dates[0].Month);
            Assert.AreEqual(1, dates[0].Day);
            Assert.AreEqual(14, dates[0].Hour);
            Assert.AreEqual(0, dates[0].Minute);
            Assert.AreEqual(10, dates[0].Second);
            Assert.AreEqual(2000, dates[0].NullableYear);
            Assert.AreEqual(1, dates[0].NullableMonth);
            Assert.AreEqual(1, dates[0].NullableDay);
            Assert.AreEqual(14, dates[0].NullableHour);
            Assert.AreEqual(0, dates[0].NullableMinute);
            Assert.AreEqual(10, dates[0].NullableSecond);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectLimitOffset(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            
            // Test-specific seed instead of ResetDb()
            var stmtList = new StatementList();
            for (var i = 0; i < 100; i++)
            {
                var name = "Product " + i;
                stmtList.Insert(DB.Products, b => b.Value(p => p.Name, name));
            }

            runner.ExecuteNonQuery(stmtList);

            var ten = 10;
            var first10 = runner.Select(DB.Products.OrderBy(builder => builder.Value(p => p.ProductId, true)).Limit(ten)).ToList();
            Assert.AreEqual(10, first10.Count);

            var second10 = runner.Select(DB.Products.OrderBy(builder => builder.Value(p => p.ProductId, true)).Offset(ten).Limit(10)).ToList();
            Assert.AreEqual(10, second10.Count);

            // Try reverse order of Limit/Offset, should not matter
            var second10Swap = runner.Select(DB.Products.OrderBy(builder => builder.Value(p => p.ProductId, true)).Limit(10).Offset(ten)).ToList();
            Assert.AreEqual(10, second10Swap.Count);

            // Multiple Offset/Limit, last should apply
            var second10Multi = runner.Select(DB.Products.OrderBy(builder => builder.Value(p => p.ProductId, true)).Offset(100000000).Limit(1).Limit(10).Offset(ten)).ToList();
            Assert.AreEqual(10, second10Multi.Count);

            Assert.AreNotEqual(first10[0].ProductId, second10[0].ProductId);

            var remainderAfter95 = runner.Select(DB.Products.OrderBy(builder => builder.Value(p => p.ProductId, true)).Offset(95)).ToList();
            Assert.AreEqual(5, remainderAfter95.Count);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectMultipleOrderBy(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);

            // Test-specific seed instead of ResetDb()
            var stmtList = new StatementList();
            stmtList.Insert(DB.Products, b => b.Value(p => p.Name, "Product"));
            var productId = stmtList.DeclareSqlVariable<int>("productId");
            stmtList.SetSqlVariable(productId, ctx => Function.LastInsertIdentity<int>(ctx));

            for (var i = 0; i < 100; i++)
            {
                var name = "Product " + i.ToString("D3");
                var sku = "SKU" + i.ToString("D3");
                var price = i % 5; // cannot use in expression: evaled too late!
                stmtList.Insert(DB.Units, b => b
                    .Value(u => u.ProductId, productId.Value)
                    .Value(p => p.Name, name)
                    .Value(p => p.Price, price)
                    .Value(p => p.UnitCode, sku));
            }

            runner.ExecuteNonQuery(stmtList);

            var ordered = runner.Select(DB.Units.OrderBy(orderBy => orderBy.Value(u => u.Price, false).Value(u => u.Name, true))).ToList();

            Assert.AreEqual(4, ordered[0].Price);
            Assert.AreEqual("Product 004", ordered[0].Name);

            Assert.AreEqual(4, ordered[1].Price);
            Assert.AreEqual("Product 009", ordered[1].Name);

            Assert.AreEqual(4, ordered[2].Price);
            Assert.AreEqual("Product 014", ordered[2].Name);
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SelectDynamicOrderBy(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);

            // Test-specific seed instead of ResetDb()
            var stmtList = new StatementList();
            stmtList.Insert(DB.Products, b => b.Value(p => p.Name, "Product"));
            var productId = stmtList.DeclareSqlVariable<int>("productId");
            stmtList.SetSqlVariable(productId, ctx => Function.LastInsertIdentity<int>(ctx));

            for (var i = 0; i < 100; i++)
            {
                var name = "Product " + i.ToString("D3");
                var sku = "SKU" + i.ToString("D3");
                var price = i % 5; // cannot use in expression: evaled too late!
                stmtList.Insert(DB.Units, b => b
                    .Value(u => u.ProductId, productId.Value)
                    .Value(p => p.Name, name)
                    .Value(p => p.Price, price)
                    .Value(p => p.UnitCode, sku));
            }

            runner.ExecuteNonQuery(stmtList);

            var ordering = new OrderByBuilder<Unit>();
            ordering.Value(u => u.Price, true).Value(u => u.Name, false);

            var ordered = runner.Select(DB.Units.OrderBy(orderBy => orderBy.Values(ordering))).ToList();

            Assert.AreEqual(0, ordered[0].Price);
            Assert.AreEqual("Product 095", ordered[0].Name);

            Assert.AreEqual(0, ordered[1].Price);
            Assert.AreEqual("Product 090", ordered[1].Name);

            Assert.AreEqual(0, ordered[2].Price);
            Assert.AreEqual("Product 085", ordered[2].Name);
        }
    }
}
