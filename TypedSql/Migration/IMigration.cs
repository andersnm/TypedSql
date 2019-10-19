using System.Collections.Generic;

namespace TypedSql.Migration
{
    public interface IMigration
    {
        /// <summary>
        /// Migrations are sorted by name, which is typically prefixed by a date.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// State of tables when the migrtation was generated.
        /// </summary>
        List<SqlTable> Tables { get; }

        void Up(SqlQueryRunner runner);
        void Down(SqlQueryRunner runner);
    }
}
