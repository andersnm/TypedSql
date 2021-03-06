﻿using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using TypedSql.PostgreSql;

namespace TypedSql.Test
{
    [TestFixture]
    public class UpdateTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateFromConstants(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Update(
                DB.Products.Where(p => p.ProductId == 1),
                (_, builder) => builder.Value(b => b.Name, "Not tonight"));
            Assert.AreEqual(1, results, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateWithJoin(Type runnerType)
        {
            var stmtList = new StatementList();

            stmtList.Update(
                DB.Units
                    .Join(
                        DB.Products,
                        (actx, a, bctx, b) => a.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new {
                            a.Name,
                            ProductName = b.Name,
                            ProductProductId = b.ProductId,
                        })
                    .Where(p => p.ProductProductId == 1),
                (up, builder) => builder.Value(b => b.Name, up.Name + " (Updated from " + up.ProductName + ")"));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteNonQuery(stmtList);
            Assert.AreEqual(3, results, "Should be 3 result");
        }
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateWithJoin2(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Update(
                DB.Inventories
                    .Join(
                        DB.Units,
                        (actx, a, bctx, b) => a.UnitId == b.UnitId,
                        (actx, a, bctx, b) => new { Inventory = a, Unit = b })
                    .Join(DB.Products,
                        (actx, a, bctx, b) => a.Unit.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new { a.Inventory, a.Unit, Product = b })
                    .Where(p => p.Product.ProductId == 1 && p.Inventory.Stock == 0),
                (up, builder) => builder.Value(b => b.Stock, 77));

            Assert.AreEqual(1, results, "Should be 1 affected row");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateDynamically(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var updater = new InsertBuilder<TypeValue>()
                .Value(p => p.StringValue, "Not tonight")
                .Value(p => p.IntValue, 47);

            updater.Value(p => p.BoolValue, true);

            var results = runner.Update(
                DB.TypeValues.Where(p => p.ByteValue == 1),
                (_, builder) => builder.Values(updater));
            Assert.AreEqual(1, results, "Should be 1 result");

            var updated = runner.Select(DB.TypeValues.Where(p => p.ByteValue == 1)).FirstOrDefault();
            Assert.AreEqual(true, updated.BoolValue, "Should be true");
            Assert.AreEqual(47, updated.IntValue, "Should be 47");
            Assert.AreEqual("Not tonight", updated.StringValue, "Should be 'Not tonight'");
            Assert.AreEqual(IntEnumType.TestValue1, updated.IntEnumValue, "Should be IntEnumValue.TestValue1");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateTwo(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var stmtList = new StatementList();
            stmtList.Update(
                DB.Products.Where(p => p.ProductId == 1),
                (_, builder) => builder.Value(b => b.Name, "Not tonight"));

            stmtList.Update(
                DB.Products.Where(p => p.ProductId == 2),
                (_, builder) => builder.Value(b => b.Name, "Nontoonyt Island"));

            var affectedRows = runner.ExecuteNonQuery(stmtList);
            Assert.AreEqual(2, affectedRows);
        }
    }
}
