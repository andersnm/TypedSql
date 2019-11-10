using System;
using System.Data.SqlClient;
using TypedSql.InMemory;
using TypedSql.MySql;
using TypedSql.SqlServer;
using TypedSql.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Npgsql;

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

            var pgcsb = new NpgsqlConnectionStringBuilder()
            {
                Host = ConfigurationBinder.GetValue<string>(config, "PostgreSql:Host"),
                Port = ConfigurationBinder.GetValue<int>(config, "PostgreSql:Port"),
                Username = ConfigurationBinder.GetValue<string>(config, "PostgreSql:Username"),
                Password = ConfigurationBinder.GetValue<string>(config, "PostgreSql:Password"),
                Database = ConfigurationBinder.GetValue<string>(config, "PostgreSql:Database"),
            };

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

            services.AddScoped(provider =>
            {
                var connection = new NpgsqlConnection(pgcsb.ConnectionString);
                connection.Open();
                return connection;
            });

            services.AddScoped(provider =>
            {
                return new PostgreSqlQueryRunner(provider.GetRequiredService<NpgsqlConnection>());
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

            var runnerType = (Type)TestContext.CurrentContext.Test.Arguments[0];
            var queryRunner = Provider.GetRequiredService(runnerType);
            if (queryRunner is SqlQueryRunner runner)
            {
                UpDb(runner);
            }
        }

        [TearDown]
        public void BaseTearDown()
        {
            var runnerType = (Type)TestContext.CurrentContext.Test.Arguments[0];
            var queryRunner = Provider.GetRequiredService(runnerType);
            if (queryRunner is SqlQueryRunner runner)
            {
                DownDb(runner);
            }

            Provider = null;
            TestScope.Dispose();
            TestScope = null;
        }

        protected void UpDb(SqlQueryRunner runner)
        {
            try
            {
                var migrator = new Migration.Migrator();
                migrator.ReadAssemblyMigrations(GetType().Assembly);
                migrator.ReadAppliedMigrations(runner);
                migrator.MigrateToLatest(runner);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected void DownDb(SqlQueryRunner runner)
        {
            try
            {
                var migrator = new Migration.Migrator();
                migrator.ReadAssemblyMigrations(GetType().Assembly);
                migrator.ReadAppliedMigrations(runner);
                while (migrator.AppliedMigrations.Count > 0)
                {
                    migrator.MigrateDown(runner);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected void ResetDb(IQueryRunner runner)
        {
            var stmtList = new StatementList();
            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "Happy T-Shirt"));

            var product1Id = stmtList.DeclareSqlVariable<int>("product1Id");
            stmtList.SetSqlVariable(product1Id, ctx => Function.LastInsertIdentity<int>(ctx));

            stmtList.Insert(DB.Products, insert => insert.Value(p => p.Name, "Test Product Without Units"));

            var product2Id = stmtList.DeclareSqlVariable<int>("product2Id");
            stmtList.SetSqlVariable(product2Id, ctx => Function.LastInsertIdentity<int>(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy XL")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 150));

            var unit1Id = stmtList.DeclareSqlVariable<int>("unit1Id");
            stmtList.SetSqlVariable(unit1Id, ctx => Function.LastInsertIdentity<int>(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy Large")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 100));

            var unit2Id = stmtList.DeclareSqlVariable<int>("unit2Id");
            stmtList.SetSqlVariable(unit2Id, ctx => Function.LastInsertIdentity<int>(ctx));

            stmtList.Insert(DB.Units, insert => insert
                .Value(p => p.Name, "Happy Small")
                .Value(p => p.ProductId, product1Id.Value)
                .Value(p => p.Price, 50));

            var unit3Id = stmtList.DeclareSqlVariable<int>("unit3Id");
            stmtList.SetSqlVariable(unit3Id, ctx => Function.LastInsertIdentity<int>(ctx));

            stmtList.Insert(DB.Inventories, insert => insert
                .Value(p => p.UnitId, unit1Id.Value)
                .Value(p => p.Stock, 10));

            stmtList.Insert(DB.Inventories, insert => insert
                .Value(p => p.UnitId, unit2Id.Value)
                .Value(p => p.Stock, 50));

            stmtList.Insert(DB.Inventories, insert => insert
                .Value(p => p.UnitId, unit3Id.Value)
                .Value(p => p.Stock, 0));

            var date1 = new DateTime(2000, 1, 1, 14, 00, 00);
            var date2 = new DateTime(2000, 1, 1, 14, 00, 10);

            var bytes = new byte[200];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)i;
            }

            // Insert some records to aggregate
            stmtList.Insert(DB.TypeValues, insert => insert
                .Value(t => t.ByteValue, (byte)1) // TODO: 
                .Value(t => t.BoolValue, false)
                .Value(t => t.DateTimeValue, date1)
                .Value(t => t.NullableDateTimeValue, null)
                .Value(t => t.DecimalValue, 1.0M)
                .Value(t => t.DoubleValue, 1.0)
                .Value(t => t.FloatValue, 1.0f)
                .Value(t => t.IntValue, 1)
                .Value(t => t.NullableIntValue, 1)
                .Value(t => t.LongValue, (long)1)
                .Value(t => t.ShortValue, (short)1)
                .Value(t => t.StringValue, "1")
                .Value(t => t.IntEnumValue, IntEnumType.TestValue1)
                .Value(t => t.BlobValue, bytes)
            );

            stmtList.Insert(DB.TypeValues, insert => insert
                .Value(t => t.ByteValue, (byte)10) // TODO: 
                .Value(t => t.BoolValue, false)
                .Value(t => t.DateTimeValue, date2)
                .Value(t => t.NullableDateTimeValue, date2)
                .Value(t => t.DecimalValue, 10.0M)
                .Value(t => t.DoubleValue, 10.0)
                .Value(t => t.FloatValue, 10.0f)
                .Value(t => t.IntValue, 10)
                .Value(t => t.NullableIntValue, 10)
                .Value(t => t.LongValue, (long)10)
                .Value(t => t.ShortValue, (short)10)
                .Value(t => t.StringValue, "10")
                .Value(t => t.IntEnumValue, IntEnumType.TestValue2)
                .Value(t => t.BlobValue, bytes)
            );

            runner.ExecuteNonQuery(stmtList);

        }

    }
}
