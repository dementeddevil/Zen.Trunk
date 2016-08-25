// -----------------------------------------------------------------------
// <copyright file="StorageQueryTest.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Zen.Trunk.Storage.Data;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Query;

namespace Zen.Trunk.StorageEngine.Tests
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Trait("Subsystem", "Storage Engine Query")]
    public class StorageQueryTest
    {
        [Fact(DisplayName = "Create master device")]
        public async Task CreateMasterDeviceTest()
        {
            using (var tracker = new TempFileTracker())
            {
                using (var manager = new MasterDatabaseDevice())
                {
                    var executive = new QueryExecutive(manager);

                    var dataFile = tracker.Get("master.mddf");
                    var logFile = tracker.Get("master_log.mlf");

                    var batch = new StringBuilder();

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
}
