using System;
using System.Linq;
using System.Linq.Expressions;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace TypedSql.Test
{
    [TestFixture]
    public class VariableTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SetVariableTypes(Type runnerType)
        {
            var stmtList = new SqlStatementList();

            var intVar = stmtList.DeclareSqlVariable<int>("intVar");
            stmtList.SetSqlVariable(intVar, ctx => 1);

            var decimalVar = stmtList.DeclareSqlVariable<decimal>("decimalVar");
            stmtList.SetSqlVariable(decimalVar, ctx => 2.23M);

            //var intNullableVar = stmtList.DeclareSqlVariable<int?>("intNullableVar");

            var stringVar = stmtList.DeclareSqlVariable<string>("stringVar");
            stmtList.SetSqlVariable(stringVar, ctx => "test");

            var select = stmtList.Select(ctx => new {
                intVar = intVar.Value,
                decimalVar = decimalVar.Value,
                //intNullableVar = intNullableVar.Value,
                stringVar = stringVar.Value
            });
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].intVar, "intVar should be 1");
            Assert.AreEqual(2.23M, results[0].decimalVar, "decimalVar should be 2.23");
            Assert.AreEqual("test", results[0].stringVar, "stringVar should be 'test'");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SetVariableFromSelect(Type runnerType)
        {
            var stmtList = new SqlStatementList();
            var testVariable = stmtList.DeclareSqlVariable<int>("test");
            stmtList.SetSqlVariable(testVariable,
                varctx => DB.Products.Where(p => p.ProductId == 1).Project((ctx, p) => p.ProductId).AsExpression(varctx));

            var select = stmtList.Select(ctx => new { Result = testVariable.Value } );
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].Result, "Selected variable should be 1");
        }
    }
}
