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
    public class InsertTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValues(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.Insert(DB.Products, insert => insert.Value(p => p.Name, "test insert product"));

            Assert.AreEqual(1, results, "Should be 1 result");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValuesSelectIdentity(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var identity = runner.Insert<Product, int>(DB.Products, insert => insert.Value(p => p.Name, "test insert product"));
            Assert.AreEqual(3, identity, "New identity should be 3");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValuesSelect(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Insert(DB.Products, DB.Units, (x, insert) => insert.Value(p => p.Name, "Product from " + x.Name));

            Assert.AreEqual(3, results, "Should be 3 results");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValuesSelectNoResults(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Insert(
                DB.Products, 
                DB.Units.Where(u => u.UnitId < 0),
                (x, insert) => insert.Value(p => p.Name, "Product from " + x.Name));

            Assert.AreEqual(0, results, "Should be 0 results");
        }
    }
}
