using System;
using System.Linq.Expressions;

namespace TypedSql
{
    public interface IIfStatement : IStatement
    {
        int EvaluateInMemory(IQueryRunner runner);
    }

    public class IfStatement : IIfStatement
    {
        public Expression<Func<bool>> TestExpression { get; }
        public Func<bool> TestExpressionFunction { get; }
        public StatementList IfStatements { get; }
        public StatementList ElseStatements { get; }

        public IfStatement(Expression<Func<bool>> testExpression, StatementList ifStatements, StatementList elseStatements)
        {
            TestExpression = testExpression;
            TestExpressionFunction = testExpression.Compile();
            IfStatements = ifStatements;
            ElseStatements = elseStatements;
        }

        public int EvaluateInMemory(IQueryRunner runner)
        {
            if (TestExpressionFunction())
            {
                return runner.ExecuteNonQuery(IfStatements);
            }
            else
            {
                return runner.ExecuteNonQuery(ElseStatements);
            }
        }

        public SqlStatement Parse(SqlQueryParser parser)
        {
            var ifStatements = parser.ParseStatementList(IfStatements);
            var elseStatements = ElseStatements != null ? parser.ParseStatementList(ElseStatements) : null;

            return new SqlIf()
            {
                Expression = parser.ParseExpression(TestExpression),
                Block = ifStatements,
                Block2 = elseStatements,
            };
        }
    }
}
