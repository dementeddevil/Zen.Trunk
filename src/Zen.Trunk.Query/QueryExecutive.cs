using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Zen.Trunk.Storage.Query
{
    /// <summary>
    /// 
    /// </summary>
    public class QueryExecutive
    {
        private readonly MasterDatabaseDevice _masterDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExecutive"/> class.
        /// </summary>
        /// <param name="masterDevice">The master device.</param>
        public QueryExecutive(MasterDatabaseDevice masterDevice)
        {
            _masterDevice = masterDevice;
        }

        /// <summary>
        /// Compiles the specified SQL command batch and returns a function that when given
        /// an execution context, will execute the batch.
        /// </summary>
        /// <param name="statementBatch">The statement batch.</param>
        /// <returns></returns>
        public Func<QueryExecutionContext, Task> CompileBatch(string statementBatch)
        {
            // Tokenise the input character stream
            var charStream = new AntlrInputStream(statementBatch);
            var lexer = new TrunkSqlLexer(charStream);

            // Build AST from the token stream
            // TODO: Process and return parser errors
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new TrunkSqlParser(tokenStream);
            var batchContext = parser.batch();

            // Validate symbol table
            var validator = new SymbolTableValidator();
            if (!batchContext.Accept(validator))
            {
                // TODO: Return collection of violations
            }

            // Build query batch pipeline from the AST
            // TODO: Determine how to detect and return semantic errors

            var visitor = new SqlBatchOperationBuilder();
            return visitor.Compile(batchContext);
            //var expression = batchContext.Accept(visitor);

            //// Create lambda expression capable of executing the expression tree
            //var runner = Expression.Lambda<Func<QueryExecutionContext, Task>>(
            //    expression,
            //    Expression.Parameter(typeof(QueryExecutionContext), "executionContext"));

            //return runner.Compile();
        }
    }
}
