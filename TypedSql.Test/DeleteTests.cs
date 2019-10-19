using System;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace TypedSql.Test
{
    [TestFixture]
    public class DeleteTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void DeleteOne(Type runnerType)
        {
            var stmtList = new StatementList();
            stmtList.Delete(
                DB.Products.Where(p => p.ProductId == 2));

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteNonQuery(stmtList);
            Assert.AreEqual(1, results, "Should be 1 result");
        }
    }
}
