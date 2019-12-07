using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TypedSql
{
    public class SelectorValue
    {
        public LambdaExpression Selector { get; set; }
        public object Value { get; set; }
    }

    public class InsertBuilder<T>
    {
        public List<SelectorValue> Selectors { get; } = new List<SelectorValue>();
        public Type BuilderType { get; } = typeof(T);

        public InsertBuilder<T> Value<TValueType>(Expression<Func<T, TValueType>> selector, TValueType value)
        {
            Selectors.Add(new SelectorValue() { Selector = selector, Value = value });
            return this;
        }

        public InsertBuilder<T> Values(InsertBuilder<T> other)
        {
            Selectors.AddRange(other.Selectors);
            return this;
        }
    }
}
