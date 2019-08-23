using System;
using System.Data.SqlClient;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace TypedSql.Test
{
    [NonParallelizable]
    public class TestDataContextFixture
    {
        private IServiceProvider RootProvider { get; set; }
        public IServiceProvider Provider { get; set; }
        public TestDataContext DB { get; set; }
        private IServiceScope TestScope { get; set; } // the scope for Provider

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var environment = Environment.GetEnvironmentVariable("TYPEDSQL_ENVIRONMENT");
            var config = new ConfigurationBuilder()
                .SetBasePath(TestContext.CurrentContext.TestDirectory)
                .AddJsonFile("appSettings.json")
                .AddJsonFile("appSettings." + environment + ".json", true)
                .Build();

            var mycsb = new MySqlConnectionStringBuilder()
            {
                Server = ConfigurationBinder.GetValue<string>(config, "MySql:Server"),
                Port = ConfigurationBinder.GetValue<uint>(config, "MySql:Port"),
                UserID = ConfigurationBinder.GetValue<string>(config, "MySql:UserID"),
                Password = ConfigurationBinder.GetValue<string>(config, "MySql:Password"),
                Database = ConfigurationBinder.GetValue<string>(config, "MySql:Database"),
                AllowUserVariables = ConfigurationBinder.GetValue<bool>(config, "MySql:AllowUserVariables"),
            };

            var mscsb = new SqlConnectionStringBuilder()
            {
                DataSource = ConfigurationBinder.GetValue<string>(config, "SqlServer:DataSource"),
                InitialCatalog = ConfigurationBinder.GetValue<string>(config, "SqlServer:InitialCatalog"),
                IntegratedSecurity = ConfigurationBinder.GetValue<bool>(config, "SqlServer:IntegratedSecurity"),
            };

            if (!mscsb.IntegratedSecurity)
            {
                mscsb.UserID = ConfigurationBinder.GetValue<string>(config, "SqlServer:UserID");
                mscsb.Password = ConfigurationBinder.GetValue<string>(config, "SqlServer:Password");
            }

            var services = new ServiceCollection();

            services.AddScoped(provider =>
            {
                var connection = new MySqlConnection(mycsb.ConnectionString);
                connection.Open();
                return connection;
            });

            services.AddScoped(provider =>
            {
                return new MySqlQueryRunner(provider.GetRequiredService<MySqlConnection>());
            });

            services.AddScoped(provider =>
            {
                return new InMemoryQueryRunner();
            });

            services.AddScoped(provider =>
            {
                var connection = new SqlConnection(mscsb.ConnectionString);
                connection.Open();
                return connection;
            });

            services.AddScoped(provider =>
            {
                return new SqlServerQueryRunner(provider.GetRequiredService<SqlConnection>());
            });


            RootProvider = services.BuildServiceProvider();
        }

        [SetUp]
        public void BaseSetUp()
        {
            // create scope
            TestScope = RootProvider.CreateScope();
            Provider = TestScope.ServiceProvider;
            DB = new TestDataContext();
        }

        [TearDown]
        public void BaseTearDown()
        {
            Provider = null;
            TestScope.Dispose();
            TestScope = null;
        }

        protected void ResetDb(IQueryRunner runner)
        {
            var formatter = new MySqlFormatter();
            DB.DropDatabase(formatter, runner);
            DB.CreateDatabase(formatter, runner);

            var stmtList = new SqlStatementList();
            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "Happy T-Shirt"));

            var product1Id = stmtList.DeclareSqlVariable<int>("product1Id");
            stmtList.SetSqlVariable(product1Id, ctx => Function.LastInsertIdentity(ctx));

            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "Test Product Without Units"));

            var product2Id = stmtList.DeclareSqlVariable<int>("product2Id");
            stmtList.SetSqlVariable(product2Id, ctx => Function.LastInsertIdentity(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy XL")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 150));

            var unit1Id = stmtList.DeclareSqlVariable<int>("unit1Id");
            stmtList.SetSqlVariable(unit1Id, ctx => Function.LastInsertIdentity(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy Large")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 100));

            var unit2Id = stmtList.DeclareSqlVariable<int>("unit2Id");
            stmtList.SetSqlVariable(unit2Id, ctx => Function.LastInsertIdentity(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy Small")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 50));

            stmtList.Insert(DB.Inventories, insert => insert
                .Value(p => p.UnitId, unit1Id.Value)
                .Value(p => p.Stock, 10));

            stmtList.Insert(DB.Inventories, insert => insert
                .Value(p => p.UnitId, unit2Id.Value)
                .Value(p => p.Stock, 50));

            runner.ExecuteNonQuery(stmtList);

        }

    }
}
