using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TypedSql
{
    public enum SqlBinaryOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        AndAlso,
        OrElse,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Like,
        Coalesce,
    }

    public abstract class SqlExpression
    {
        public abstract Type GetExpressionType();
    }

    public class SqlTableExpression : SqlExpression
    {
        public SqlSubQueryResult TableResult { get; set; }

        public override Type GetExpressionType()
        {
            return typeof(SqlSubQueryResult);
        }
    }

    public class SqlSelectorExpression : SqlExpression
    {
        public SqlExpression SelectorExpression { get; set; }

        public override Type GetExpressionType()
        {
            return SelectorExpression.GetExpressionType();
        }
    }

    public class SqlTableFieldExpression : SqlExpression
    {
        public SqlTableFieldMember TableFieldRef { get; set; }

        public override Type GetExpressionType()
        {
            return TableFieldRef.FieldType;
        }
    }

    public class SqlJoinFieldExpression : SqlExpression
    {
        public SqlJoinFieldMember JoinFieldRef { get; set; }

        public override Type GetExpressionType()
        {
            return JoinFieldRef.FieldType;
        }
    }

    public class SqlBinaryExpression : SqlExpression
    {
        public SqlExpression Left { get; set; }
        public SqlExpression Right { get; set; }
        public SqlBinaryOperator Op { get; set; }

        public override Type GetExpressionType()
        {
            // Handle boolean expressions
            switch (Op)
            {
                case SqlBinaryOperator.Equal:
                case SqlBinaryOperator.NotEqual:
                case SqlBinaryOperator.GreaterThan:
                case SqlBinaryOperator.GreaterThanOrEqual:
                case SqlBinaryOperator.LessThan:
                case SqlBinaryOperator.LessThanOrEqual:
                case SqlBinaryOperator.AndAlso:
                case SqlBinaryOperator.OrElse:
                    return typeof(bool);
            }

            // Assume any other operators require same type on both sides
            var leftType = Left.GetExpressionType();
            var rightType = Right.GetExpressionType();

            var leftTypeInfo = leftType.GetTypeInfo();
            var leftNullable = leftTypeInfo.IsGenericType && leftTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (leftNullable)
            {
                leftType = Nullable.GetUnderlyingType(leftType);
            }

            var rightTypeInfo = rightType.GetTypeInfo();
            var rightNullable = rightTypeInfo.IsGenericType && rightTypeInfo.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (rightNullable)
            {
                rightType = Nullable.GetUnderlyingType(rightType);
            }

            if (leftType != rightType)
            {
                throw new InvalidOperationException("BinaryExpression requires same type on both sides");
            }

            return leftType;
        }
    }

    public class SqlCastExpression : SqlExpression
    {
        public SqlExpression Operand { get; set; }
        public Type TargetType { get; set; }

        public override Type GetExpressionType()
        {
            return TargetType;
        }
    }

    public class SqlNegateExpression : SqlExpression
    {
        public SqlExpression Operand { get; set; }

        public override Type GetExpressionType()
        {
            return Operand.GetExpressionType();
        }
    }

    public class SqlNotExpression : SqlExpression
    {
        public SqlExpression Operand { get; set; }

        public override Type GetExpressionType()
        {
            return Operand.GetExpressionType();
        }
    }

    public class SqlConditionalExpression : SqlExpression
    {
        public SqlExpression Test { get; set; }
        public SqlExpression IfTrue { get; set; }
        public SqlExpression IfFalse { get; set; }

        public override Type GetExpressionType()
        {
            // TOOD: check same type as IfFalse?
            return IfTrue.GetExpressionType();
        }
    }

    public class SqlConstantExpression : SqlExpression
    {
        public object Value { get; set; }
        public Type ConstantType { get; set; }

        public override Type GetExpressionType()
        {
            return ConstantType;
        }
    }

    public class SqlConstantArrayExpression : SqlExpression
    {
        public List<object> Value { get; set; }

        public override Type GetExpressionType()
        {
            throw new NotImplementedException();
        }
    }

    public class SqlPlaceholderExpression : SqlExpression
    {
        public SqlPlaceholder Placeholder { get; set; }

        public override Type GetExpressionType()
        {
            return Placeholder.ValueType;
        }
    }

    public class SqlCallExpression : SqlExpression
    {
        public MethodInfo Method { get; set; }
        public List<SqlExpression> Arguments { get; set; }

        public override Type GetExpressionType()
        {
            return Method.ReturnType;
        }
    }

    public class SqlSelectExpression : SqlExpression
    {
        public SqlQuery Query { get; set; }

        public override Type GetExpressionType()
        {
            return Query.SelectResult.Members[0].FieldType;
        }
    }
}
