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
            var stmtList = new StatementList();

            var intVar = stmtList.DeclareSqlVariable<int>("intVar");
            stmtList.SetSqlVariable(intVar, ctx => 1);

            var decimalVar = stmtList.DeclareSqlVariable<decimal>("decimalVar");
            stmtList.SetSqlVariable(decimalVar, ctx => 2.23M);

            var intNullableVar = stmtList.DeclareSqlVariable<int?>("intNullableVar");
            stmtList.SetSqlVariable(intNullableVar, ctx => 42);

            var stringVar = stmtList.DeclareSqlVariable<string>("stringVar");
            stmtList.SetSqlVariable(stringVar, ctx => "test");

            var select = stmtList.Select(ctx => new {
                intVar = intVar.Value,
                decimalVar = decimalVar.Value,
                intNullableVar = intNullableVar.Value,
                stringVar = stringVar.Value
            });
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].intVar, "intVar should be 1");
            Assert.AreEqual(42, results[0].intNullableVar, "intNullableVar should be 42");
            Assert.AreEqual(2.23M, results[0].decimalVar, "decimalVar should be 2.23");
            Assert.AreEqual("test", results[0].stringVar, "stringVar should be 'test'");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SetVariableFromSelect(Type runnerType)
        {
            var stmtList = new StatementList();
            var testVariable = stmtList.DeclareSqlVariable<int>("test");
            stmtList.SetSqlVariable(testVariable,
                varctx => DB.Products.Where(p => p.ProductId == 1).Project((ctx, p) => p.ProductId).AsExpression(varctx));

            var select = stmtList.Select(ctx => new { Result = testVariable.Value });
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(1, results[0].Result, "Selected variable should be 1");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        [Ignore("TODO: IF in stored procedures")]
        public void TestIf(Type runnerType)
        {
            var stmtList = new StatementList();
            var testVariable = stmtList.DeclareSqlVariable<int>("test");
            stmtList.SetSqlVariable(testVariable, varctx => 1);

            var ifScope = new StatementList(stmtList);
            var elseScope = new StatementList(stmtList);
            stmtList.If(() => testVariable.Value == 1, ifScope, elseScope);

            var select = ifScope.Select(ctx => new { Result = "IF" });
            elseScope.Select(ctx => new { Result = "ELSE" });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            // var sql = runner.GetSql(stmtList, out _);
            // throw new Exception(sql);

            // TODO: this executes only whts inside the IF
            var results = runner.ExecuteQuery(select).ToList();
            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual("IF", results[0].Result, "Selected result should be 'IF'");
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(InMemoryQueryRunner))]
        public void SetVariableIdentityExpression(Type runnerType)
        {
            var stmtList = new StatementList();
            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "test insert product"));
            var identity = stmtList.DeclareSqlVariable<int>("myident");
            stmtList.SetSqlVariable(identity, (ctx) => Function.LastInsertIdentity<int>(ctx));

            var select = stmtList.Select(ctx => new { Identity = identity.Value });

            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var results = runner.ExecuteQuery(select).ToList();

            Assert.AreEqual(1, results.Count, "Should be 1 result");
            Assert.AreEqual(3, results[0].Identity, "New identity should be 3");
        }
    }
}
