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

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void UpdateDynamically(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            // Insert a record to update
            runner.Insert(DB.TypeValues, insert => insert
                .Value(t => t.ByteValue, (byte)1) // TODO: 
                .Value(t => t.BoolValue, false)
                .Value(t => t.DateTimeValue, DateTime.Now)
                .Value(t => t.DecimalValue, 0.0M)
                .Value(t => t.DoubleValue, 0.0)
                .Value(t => t.FloatValue, 0.0f)
                .Value(t => t.IntValue, 0)
                .Value(t => t.LongValue, (long)0)
                .Value(t => t.ShortValue, (short)0)
                .Value(t => t.StringValue, string.Empty)
            );

            var updater = new InsertBuilder<TypeValue>()
                .Value(p => p.StringValue, "Not tonight")
                .Value(p => p.IntValue, 47);

            updater.Value(p => p.BoolValue, true);

            var results = runner.Update(
                DB.TypeValues.Where(p => p.ByteValue == 1),
                (_, builder) => builder.Values(updater));
            Assert.AreEqual(1, results, "Should be 1 result");
        }

    }
}
