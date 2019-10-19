using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TypedSql.Migration
{
    [SqlTable(Name = "_TypedSqlMigrationHistory")]
    public class Migration
    {
        [SqlString(Length = 128)]
        public string Name { get; set; }
        
        [SqlString(Length = 64)]
        public string Version { get; set; }
    }

    internal class MigrationContext : DatabaseContext {
        public FromQuery<Migration> Migrations { get; set; }
    }

    public class Migrator
    {
        internal MigrationContext Context { get; } = new MigrationContext();
        public List<IMigration> Migrations { get; private set; } = new List<IMigration>();
        public List<Migration> AppliedMigrations { get; private set; } = new List<Migration>();

        /// <summary>
        /// Scan for implementations of the IMigration interface in the provided assembly and order by name
        /// </summary>
        public void ReadAssemblyMigrations(Assembly migrationAssembly)
        {
            // 
            foreach (var exportedType in migrationAssembly.ExportedTypes)
            {
                if (exportedType.GetTypeInfo().GetInterfaces().Contains(typeof(IMigration)))
                {
                    var previousMigration = (IMigration)Activator.CreateInstance(exportedType);
                    Migrations.Add(previousMigration);
                }
            }

            Migrations = Migrations.OrderBy(m => m.Name).ToList();
        }

        public void ReadAppliedMigrations(SqlQueryRunner runner)
        {
            var stmtList = new StatementList();
            var select = stmtList.Select(Context.Migrations.OrderBy(true, m => m.Name));

            try
            {
                AppliedMigrations = runner.ExecuteQuery(select).ToList();
            }
            catch (Exception)
            {
                // Ignore exception when the table does not exist.
                // Should ideally not ignore more serious errors, or even more ideally 
                // check db schema if table exists
            }
        }

        public void EnsureMigrationTable(SqlQueryRunner runner)
        {
            var stmtList = new StatementList();
            stmtList.Queries.Add(new CreateTableStatement(Context.Migrations));
            try
            {
                runner.ExecuteNonQuery(stmtList);
            }
            catch (Exception)
            {
                // Ignore exception when the table already exists.
            }
        }

        public void MigrateToLatest(SqlQueryRunner runner)
        {
            if (AppliedMigrations.Count > Migrations.Count)
            {
                throw new InvalidOperationException("There are more applied migrations than actual migrations.");
            }

            if (AppliedMigrations.Count == 0)
            {
                EnsureMigrationTable(runner);
            }

            for (var i = 0; i < Migrations.Count; i++)
            {
                var assemblyMigration = Migrations[i];
                if (i < AppliedMigrations.Count)
                {
                    var appliedMigration = AppliedMigrations[i];
                    if (appliedMigration.Name != assemblyMigration.Name)
                    {
                        throw new InvalidOperationException("Have applied migration " + appliedMigration.Name + ", expected " + assemblyMigration.Name);
                    }

                    continue;
                }

                assemblyMigration.Up(runner);

                var stmtList = new StatementList();
                stmtList.Insert(Context.Migrations, 
                    insert => insert
                        .Value(m => m.Name, assemblyMigration.Name)
                        .Value(m => m.Version, "Version X"));
                runner.ExecuteNonQuery(stmtList);
            }
        }

        public void MigrateDown(SqlQueryRunner runner)
        {
            if (AppliedMigrations.Count == 0) 
            {
                return;
            }

            var lastMigration = Migrations[AppliedMigrations.Count -1];
            var appliedMigration = AppliedMigrations.Last();
            if (appliedMigration.Name != lastMigration.Name)
            {
                throw new InvalidOperationException("Have applied migration " + appliedMigration.Name + ", expected " + lastMigration.Name);
            }

            lastMigration.Down(runner);
            AppliedMigrations.Remove(appliedMigration);

            var stmtList = new StatementList();
            stmtList.Delete(Context.Migrations.Where(m => m.Name == appliedMigration.Name && m.Version == appliedMigration.Version));
            runner.ExecuteNonQuery(stmtList);
        }
    }
}
