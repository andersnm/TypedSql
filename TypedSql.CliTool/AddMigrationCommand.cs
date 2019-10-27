using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using TypedSql.Migration;

namespace TypedSql.CliTool
{
    public class AddMigrationCommand
    {
        public static void RegisterWithApp(CommandLineApplication app)
        {
            app.Command("add-migration", cmdApp =>
            {
                var optionAssembly = cmdApp.Option("-a|--assembly <ASSEMBLY>", "Assembly containing the database context and optional previous migrations", CommandOptionType.SingleValue);
                var optionName = cmdApp.Option("-n|--name <NAME>", "Unique migration name", CommandOptionType.SingleValue);
                var optionOutput = cmdApp.Option("-o|--output <PATH>", "Output path to put generated files. Defaults to ./Migrations", CommandOptionType.SingleValue);

                cmdApp.OnExecute(() =>
                {
                    if (!optionName.HasValue() || string.IsNullOrEmpty(optionName.Value()))
                    {
                        Console.WriteLine("Migration name must be specified by -n parameter");
                        cmdApp.ShowHelp();
                        return;
                    }

                    var assemblyName = optionAssembly.Value();
                    var output = optionOutput.Value();
                    if (string.IsNullOrEmpty(output))
                    {
                        output = "Migrations";
                    }

                    Directory.CreateDirectory(output);

                    AddMigration(assemblyName, optionName.Value(), output);
                });
            });
        }

        static void AddMigration(string assemblyName, string migrationName, string outputPath)
        {
            var assembly = Assembly.LoadFrom(assemblyName);
            List<SqlTable> migration = null;
            List<IMigration> migrations = new List<IMigration>();
            foreach (var exportedType in assembly.ExportedTypes)
            {
                if (exportedType.BaseType == typeof(DatabaseContext))
                {
                    var context = (DatabaseContext)Activator.CreateInstance(exportedType);
                    migration = ParseMetadata(context);
                }

                if (exportedType.GetInterfaces().Contains(typeof(IMigration)))
                {
                    if (migrationName == exportedType.Name)
                    {
                        throw new InvalidOperationException("The migration name must be unique: " + migrationName);
                    }

                    var previousMigration = (IMigration)Activator.CreateInstance(exportedType);
                    migrations.Add(previousMigration);
                }
            }

            if (migration == null)
            {
                throw new InvalidOperationException("Could not find any classes derived from TypedSql.DatabaseContext");
            }

            migrations = migrations.OrderBy(m => m.Name).ToList();
            var lastMigration = migrations.LastOrDefault()?.Tables ?? new List<SqlTable>();

            var existingMigration = migrations.Where(m => m.Name == migrationName).FirstOrDefault();
            if (existingMigration != null)
            {
                throw new InvalidOperationException("Migration " + migrationName + " exists");
            }

            var uniqueId = DateTime.Now.ToString("yyyyMMddHHmm") + "_" + migrationName;

            var writer = new StringBuilder();
            GenerateMetadataClass(uniqueId, migrationName, migration, writer);
            File.WriteAllText(Path.Combine(outputPath, uniqueId + ".Schema.cs"), writer.ToString(), Encoding.UTF8);

            writer.Clear();

            var upStatements = SqlTableComparer.CompareTables(lastMigration, migration);
            var downStatements = SqlTableComparer.CompareTables(migration, lastMigration);

            GenerateUpDownClass(uniqueId, migrationName, upStatements, downStatements, writer);
            File.WriteAllText(Path.Combine(outputPath, uniqueId + ".cs"), writer.ToString(), Encoding.UTF8);
        }

        static List<SqlTable> ParseMetadata(DatabaseContext context)
        {
            var parser = new SqlSchemaParser();
            var metadata = new List<SqlTable>();
            foreach (var fromQuery in context.FromQueries)
            {
                var stmt = parser.ParseCreateTable(fromQuery);
                var columns = stmt.Columns;

                var foreignKeys = new List<SqlForeignKey>();
                foreach (var foreignKey in fromQuery.ForeignKeys)
                {
                    var foreignKeyStmt = parser.ParseAddForeignKey(fromQuery, foreignKey);
                    foreignKeys.Add(foreignKeyStmt.ForeignKey);
                }

                var indices = new List<SqlIndex>();
                foreach (var index in fromQuery.Indices)
                {
                    var indexStmt = parser.ParseAddIndex(fromQuery, index);
                    indices.Add(indexStmt.Index);
                }

                metadata.Add(new SqlTable()
                {
                    TableName = fromQuery.TableName,
                    Columns = columns,
                    ForeignKeys = foreignKeys,
                    Indices = indices,
                });
            }

            return metadata;
        }

        static void GenerateMetadataClass(string uniqueId, string identifier, List<SqlTable> next, StringBuilder writer)
        {
            writer.AppendLine("using System;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine("using TypedSql;");
            writer.AppendLine("using TypedSql.Migration;");
            writer.AppendLine("using TypedSql.Schema;");
            writer.AppendLine();
            writer.AppendLine("public partial class " + identifier + " : IMigration");
            writer.AppendLine("{");
            writer.AppendLine("    public string Name => \"" + uniqueId + "\";");
            writer.AppendLine();
            writer.Append("    public List<SqlTable> Tables => ");
            ObjectSerializer.SerializeObject(next, 1, writer);
            writer.AppendLine(";");
            writer.AppendLine("}");
        }

        static void GenerateUpDownClass(string uniqueId, string identifier, List<SqlStatement> upStatements, List<SqlStatement> downStatements, StringBuilder writer)
        {
            writer.AppendLine("using System;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine("using TypedSql;");
            writer.AppendLine("using TypedSql.Migration;");
            writer.AppendLine("using TypedSql.Schema;");
            writer.AppendLine();
            writer.AppendLine("public partial class " + identifier + " : IMigration");
            writer.AppendLine("{");

            writer.AppendLine("    public void Up(SqlQueryRunner runner)");
            writer.AppendLine("    {");
            writer.AppendLine("        var stmtList = new List<SqlStatement>();");

            foreach (var stmt in upStatements)
            {
                writer.Append("        stmtList.Add(");
                ObjectSerializer.SerializeObject(stmt, 2, writer);
                writer.AppendLine(");");
            }

            writer.AppendLine("        runner.ExecuteNonQuery(stmtList, new List<KeyValuePair<string, object>>());");
            writer.AppendLine("    }");
            writer.AppendLine();

            writer.AppendLine("    public void Down(SqlQueryRunner runner)");
            writer.AppendLine("    {");
            writer.AppendLine("        var stmtList = new List<SqlStatement>();");

            foreach (var stmt in downStatements)
            {
                writer.Append("        stmtList.Add(");
                ObjectSerializer.SerializeObject(stmt, 2, writer);
                writer.AppendLine(");");
            }

            writer.AppendLine("        runner.ExecuteNonQuery(stmtList, new List<KeyValuePair<string, object>>());");
            writer.AppendLine("    }");
            writer.AppendLine("}");
        }
    }
}
