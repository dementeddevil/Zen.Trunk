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
        /// Executes the specified statement batch.
        /// </summary>
        /// <param name="statementBatch">The statement batch.</param>
        /// <param name="onlyPrepare">if set to <c>true</c> [only prepare].</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string statementBatch, bool onlyPrepare = false)
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
            var visitor = new SqlBatchOperationBuilder(_masterDevice);
            var expression = batchContext.Accept(visitor);

            if (onlyPrepare)
            {
                return;
            }

            // Walk the batches and execute each one
            var runner = Expression.Lambda<Func<ExecutionContext, Task>>(
                expression,
                Expression.Parameter(typeof(ExecutionContext), "executionContext"));

            var executionContext = new ExecutionContext(_masterDevice);
            await runner.Compile()(executionContext).ConfigureAwait(false);
        }
    }
}
