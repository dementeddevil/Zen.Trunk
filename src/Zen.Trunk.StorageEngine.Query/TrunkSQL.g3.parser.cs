namespace Zen.Trunk.Storage.Query
{
	using System;
	using System.Linq;

	partial class TrunkSQLParser
	{
		private static readonly string[] fixedSizeDataTypes =
			{
				"bool",
				"short",
				"int",
				"long",
				"datetime",
				"date",
				"time",
 				"guid",
				"timestamp",
				"float",
				"double",
				"money",
			};
		private static readonly string[] variableSizeDataTypes =
			{
				"byte",
				"char",
				"nchar",
				"varbyte",
				"varchar",
				"nvarchar",
			};

		private bool IsValidDatatype(string typeName, bool isVariableLength)
		{
			bool found;
			if (!isVariableLength)
			{
				found = fixedSizeDataTypes.Any((item) =>
					item.Equals(typeName, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				found = variableSizeDataTypes.Any((item) =>
					item.Equals(typeName, StringComparison.OrdinalIgnoreCase));
			}
			return found;
		}
	}
}