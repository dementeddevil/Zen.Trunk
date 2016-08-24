namespace Zen.Trunk.Storage.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Antlr4.Runtime;
    using Antlr4.Runtime.Misc;
    using Antlr4.Runtime.Tree;
    using Zen.Trunk.Storage.Data;

    [CLSCompliant(false)]
	public class QueryExecutive
	{
		private MasterDatabaseDevice _masterDevice;

		public QueryExecutive(MasterDatabaseDevice masterDevice)
		{
			_masterDevice = masterDevice;
		}

		public void Execute(string statementBatch)
		{
            // Tokenise the input character stream
			var charStream = new AntlrInputStream(statementBatch);
			var lexer = new TrunkSqlLexer(charStream);

			// Build AST from the token stream
			var tokenStream = new CommonTokenStream(lexer);
			var parser = new TrunkSqlParser(tokenStream);
			var compileUnit = parser.tsql_file();

			// Build query batch pipeline from the AST
		    var listener = new QueryTreeListener(_masterDevice);
		    listener.EnterTsql_file(compileUnit);
		}
	}

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to build a list of batches based on
    /// the AST that is walked.
    /// Each batch will create a transaction of an appropriate isolation level
    /// and execute a series of asynchronous blocks.
    /// Each block is an asynchronous operation or composite (where BEGIN/END is used).
    /// </remarks>
    public class QueryTreeListener : TrunkSqlBaseListener
    {
        private readonly MasterDatabaseDevice _masterDatabase;

        public QueryTreeListener(MasterDatabaseDevice masterDatabase)
        {
            _masterDatabase = masterDatabase;
        }

        public override void EnterBatch([NotNull] TrunkSqlParser.BatchContext context)
        {
            base.EnterBatch(context);
        }

        public override void ExitBatch([NotNull] TrunkSqlParser.BatchContext context)
        {
            base.ExitBatch(context);
        }
    }
}
