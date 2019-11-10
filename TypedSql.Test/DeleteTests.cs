using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using TypedSql.PostgreSql;

namespace TypedSql.Test
{
    [TestFixture]
    public class DeleteTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void DeleteOne(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Delete(
                DB.Products.Where(p => p.ProductId == 2));
            Assert.AreEqual(1, results, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void DeleteWithJoin(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Delete(
                DB.Inventories
                    .Join(
                        DB.Units, 
                        (actx, a, bctx, b) => a.UnitId == b.UnitId,
                        (actx, a, bctx, b) => new { Inventory = a, Unit = b })
                    .Join(DB.Products,
                        (actx, a, bctx, b) => a.Unit.ProductId == b.ProductId,
                        (actx, a, bctx, b) => new { a.Inventory, a.Unit, Product = b })
                    .Where(p => p.Product.ProductId == 1 && p.Inventory.Stock == 0));
            Assert.AreEqual(1, results, "Should be 1 affected row");
        }
    }
}
