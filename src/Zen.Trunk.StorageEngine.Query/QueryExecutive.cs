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
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new TrunkSqlParser(tokenStream);
            var compileUnit = parser.tsql_file();

            // Build query batch pipeline from the AST
            var visitor = new SqlBatchOperationBuilder(_masterDevice);
            var expression = compileUnit.Accept(visitor);

            if (onlyPrepare)
            {
                return;
            }

            // Walk the batches and execute each one
            var executionContext = new ExecutionContext(_masterDevice);
            await expression.Compile()(executionContext).ConfigureAwait(false);
        }
    }
}
