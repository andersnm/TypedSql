using System;
using System.Collections.Generic;
using System.Reflection;

namespace TypedSql
{
    public abstract class DatabaseContext
    {
        public DatabaseContext()
        {
            var typeInfo = GetType().GetTypeInfo();
            var properties = typeInfo.GetProperties();

            foreach (var property in properties)
            {
                if (typeof(IFromQuery).GetTypeInfo().IsAssignableFrom(property.PropertyType))
                {
                    var fromQuery = (IFromQuery)Activator.CreateInstance(property.PropertyType, this);
                    property.SetValue(this, fromQuery);
                    FromQueries.Add(fromQuery);
                }
            }
        }

        public List<IFromQuery> FromQueries { get; } = new List<IFromQuery>();
    }
}
