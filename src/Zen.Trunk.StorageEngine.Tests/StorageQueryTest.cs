// -----------------------------------------------------------------------
// <copyright file="StorageQueryTest.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.StorageEngine.Tests
{
	using System.IO;
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage.Data;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Storage.Query;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	[TestClass]
	public class StorageQueryTest
	{
		private static TestContext Context
		{
			get;
			set;
		}

		[ClassInitialize()]
		public static void MyClassInitialize(TestContext testContext)
		{
			Context = testContext;
		}

		[TestMethod]
		[TestCategory("Storage Engine: Query Executive")]
		public async Task CreateMasterDeviceTest()
		{
			using (var manager = new MasterDatabaseDevice())
			{
				QueryExecutive executive = new QueryExecutive(manager);

				string dataFile = Path.Combine(
					Context.TestRunResultsDirectory,
					"master.mddf");
				string logFile = Path.Combine(
					Context.TestRunResultsDirectory,
					"master_log.mlf");

				StringBuilder batch = new StringBuilder();

				TrunkTransactionContext.BeginTransaction(manager);
				batch.AppendLine("create database master");
				batch.AppendLine("\ton");
				batch.AppendLine("\t\tprimary");
				batch.AppendLine("\t\t(");
				batch.AppendLine("\t\t\tname=master,");
				batch.AppendFormat("\t\t\tfilename=\'{0}\',\n", dataFile);
				batch.AppendLine("\t\t\tsize=1024KB");
				batch.AppendLine("\t\t)");
				batch.AppendLine("\tlog on");
				batch.AppendLine("\t\t(");
				batch.AppendLine("\t\t\tname=master_log,");
				batch.AppendFormat("\t\t\tfilename=\'{0}\',\n", logFile);
				batch.AppendLine("\t\t\tsize=1024KB");
				batch.AppendLine("\t\t)");
				batch.AppendLine("go");
				await executive.Execute(batch.ToString());
				await TrunkTransactionContext.Commit();

				batch.Clear();

				TrunkTransactionContext.BeginTransaction(manager);
				batch.AppendLine("create table testtable");
				batch.AppendLine("\t(");
				batch.AppendLine("\tid int not null identity(1,1),");
				batch.AppendLine("\tblit nvarchar(128) not null,");
				batch.AppendLine("\tdesc nvarchar(1000) null");
				batch.AppendLine("\t)");
				batch.AppendLine("\ton default");
				batch.AppendLine("go");
				await executive.Execute(batch.ToString());
				await TrunkTransactionContext.Commit();

				await manager.Close();
			}
		}
	}
}
