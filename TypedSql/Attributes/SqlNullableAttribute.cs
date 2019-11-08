using System;
using System.Collections.Generic;
using System.Text;

namespace TypedSql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlNullableAttribute : Attribute
    {
    }
}
