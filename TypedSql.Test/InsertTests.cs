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
    public class InsertTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
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
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValuesSelectIdentity(Type runnerType)
        {
            var stmtList = new StatementList();
            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "test insert product"));
            var identity = stmtList.DeclareSqlVariable<int>("myident");
            stmtList.SetSqlVariable(identity, (ctx) => Function.LastInsertIdentity(ctx));

            var select = stmtList.Select(ctx => new { Identity = identity.Value });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);
            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(3, results[0].Identity, "New identity should be 3");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void InsertValuesSelect(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.Insert(DB.Products, DB.Units, (x, insert) => insert.Value(p => p.Name, "Product from " + x.Name));

            Assert.AreEqual(3, results, "Should be 3 results");
        }
    }
}
