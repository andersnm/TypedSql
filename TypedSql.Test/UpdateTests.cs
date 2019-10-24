using System;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace TypedSql.Test
{
    [TestFixture]
    public class UpdateTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
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
    }
}
