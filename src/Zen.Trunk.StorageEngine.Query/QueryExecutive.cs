namespace Zen.Trunk.Storage.Query
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Antlr.Runtime;
	using Antlr.Runtime.Tree;
	using Zen.Trunk.Storage.Data;

	[CLSCompliant(false)]
	public class QueryExecutive
	{
		/// <summary>
		/// Look ahead for tokenizing is all lowercase, whereas the original case of an input stream is preserved.
		///</summary>
		private class CaseInsensitiveStringStream : ANTLRStringStream
		{
			public CaseInsensitiveStringStream()
			{
			}

			public CaseInsensitiveStringStream(string input)
				: base(input)
			{
			}

			public CaseInsensitiveStringStream(string input, string sourceName)
				: base(input, sourceName)
			{
			}

			public CaseInsensitiveStringStream(
				char[] data, int numberOfActualCharsInArray)
				: base(data, numberOfActualCharsInArray)
			{
			}

			// Only the lookahead is converted to lowercase. The original case is preserved in the stream.
			public override int LA(int i)
			{
				if (i == 0)
				{
					return 0;
				}

				if (i < 0)
				{
					i++;
				}

				if (((p + i) - 1) >= n)
				{
					return (int)CharStreamConstants.EndOfFile;
				}

				// This is how "case insensitive" is defined, i.e., could also use a special culture...
				return Char.ToLowerInvariant(data[(p + i) - 1]);
			}
		}

		private MasterDatabaseDevice _masterDevice;

		public QueryExecutive(MasterDatabaseDevice masterDevice)
		{
			_masterDevice = masterDevice;
		}

		public Task Execute(string statementBatch)
		{
			// Tokenise the input character stream
			ANTLRStringStream charStream = new CaseInsensitiveStringStream(
				statementBatch, "TestSource");
			TrunkSQLLexer lexer = new TrunkSQLLexer(charStream);

			// Build AST from the token stream
			CommonTokenStream tokenStream = new CommonTokenStream(lexer);
			TrunkSQLParser parser = new TrunkSQLParser(tokenStream);
			var compileUnit = parser.compileUnit();

			// Build query batch pipeline from the AST
			CommonTree ast = (CommonTree)compileUnit.Tree;
			CommonTreeNodeStream astNodes = new CommonTreeNodeStream(ast);
			TrunkSQLPipeline walker = new TrunkSQLPipeline(astNodes);
			walker.MasterDatabase = _masterDevice;
			return walker.compileUnit();
		}
	}
}
