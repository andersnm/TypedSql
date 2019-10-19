using McMaster.Extensions.CommandLineUtils;

namespace TypedSql.CliTool
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication()
            {
                Name = "typedsql",
                Description = "Migrations for TypedSql",
            };
            
            app.HelpOption(true);

            AddMigrationCommand.RegisterWithApp(app);

            app.OnExecute(() =>
            {
                app.ShowHelp();
            });

            return app.Execute(args);
        }
    }
}
