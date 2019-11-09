using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TypedSql.MySql;
using TypedSql.PostgreSql;
using TypedSql.SqlServer;

namespace TypedSql.Test
{
    [TestFixture]
    public class SqlSchemaTests : TestDataContextFixture
    {
        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        public void TestDropAndCreateTable(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var sqlRunner = (SqlQueryRunner)runner;
            var parser = new SqlSchemaParser();

            var dropTable = parser.ParseDropTable(DB.TypeValues);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { dropTable }, new List<KeyValuePair<string, object>>());

            var createTable = parser.ParseDropTable(DB.TypeValues);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { createTable }, new List<KeyValuePair<string, object>>());
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        public void TestCreateDropColumn(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var sqlRunner = (SqlQueryRunner)runner;
            var parser = new SqlSchemaParser();

            var column = new Schema.Column()
            {
                SqlName = "COL_TEST",
                BaseType = typeof(int),
                Nullable = true,
                OriginalType = typeof(int?),
                SqlType = new Schema.SqlTypeInfo(),
            };

            var addColumn = parser.ParseAddColumn(DB.Units, column);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { addColumn }, new List<KeyValuePair<string, object>>());

            var dropColumn = parser.ParseDropColumn(DB.Units, column);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { dropColumn }, new List<KeyValuePair<string, object>>());
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        public void TestCreateDropForeignKey(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var sqlRunner = (SqlQueryRunner)runner;
            var parser = new SqlSchemaParser();

            var foreignKey = new Schema.ForeignKey()
            {
                ReferenceTableType = typeof(Product),
                Name = "FK_TEST",
                Columns = new List<string>() { nameof(Unit.ProductId) },
                ReferenceColumns = new List<string>() { nameof(Product.ProductId) }
            };

            // TODO: add default fk in test context, and switch the order here
            var addForeignKey = parser.ParseAddForeignKey(DB.Units, foreignKey);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { addForeignKey }, new List<KeyValuePair<string, object>>());

            var dropForeignKey = parser.ParseDropForeignKey(DB.Units, foreignKey);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { dropForeignKey }, new List<KeyValuePair<string, object>>());
        }

        [Test]
        [TestCase(typeof(MySqlQueryRunner))]
        [TestCase(typeof(SqlServerQueryRunner))]
        [TestCase(typeof(PostgreSqlQueryRunner))]
        public void TestCreateDropIndex(Type runnerType)
        {
            var runner = (IQueryRunner)Provider.GetRequiredService(runnerType);
            ResetDb(runner);

            var sqlRunner = (SqlQueryRunner)runner;
            var parser = new SqlSchemaParser();

            var index = new Schema.Index()
            {
                Name = "IX_TEST",
                Columns = new List<string>() { nameof(TypeValue.IntValue) },
                Unique = true,
            };

            var addIndex = parser.ParseAddIndex(DB.TypeValues, index);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { addIndex }, new List<KeyValuePair<string, object>>());

            var dropIndex = parser.ParseDropIndex(DB.TypeValues, index);
            sqlRunner.ExecuteNonQuery(new List<SqlStatement>() { dropIndex }, new List<KeyValuePair<string, object>>());
        }
    }
}
