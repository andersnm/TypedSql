using System;
using System.Collections.Generic;
using System.Reflection;

namespace TypedSql
{
    public abstract class DatabaseContext
    {
        internal List<IFromQuery> FromQueries { get; } = new List<IFromQuery>();

        public DatabaseContext()
        {
            var typeInfo = GetType().GetTypeInfo();
            var properties = typeInfo.GetProperties();

            foreach (var property in properties)
            {
                if (typeof(IFromQuery).GetTypeInfo().IsAssignableFrom(property.PropertyType))
                {
                    var fromQuery = (IFromQuery)Activator.CreateInstance(property.PropertyType);
                    property.SetValue(this, fromQuery);
                    FromQueries.Add(fromQuery);
                }
            }
        }

        public void CreateDatabase(IFormatter formatter, IQueryRunner runner)
        {
            var stmtList = new SqlStatementList();

            foreach (var table in FromQueries)
            {
                stmtList.Add(new CreateTableStatement(table));
            }

            runner.ExecuteNonQuery(stmtList);
        }

        public void DropDatabase(IFormatter formatter, IQueryRunner runner)
        {
            var stmtList = new SqlStatementList();

            foreach (var table in FromQueries)
            {
                stmtList.Add(new DropTableStatement(table));
            }

            runner.ExecuteNonQuery(stmtList);
        }
    }
}
