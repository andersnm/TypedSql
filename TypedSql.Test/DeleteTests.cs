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
    }
}
