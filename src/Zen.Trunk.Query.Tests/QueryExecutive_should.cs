// -----------------------------------------------------------------------
// <copyright file="StorageQueryTest.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using Autofac;
using FluentAssertions;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Query;
using Zen.Trunk.VirtualMemory.Tests;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [Trait("Subsystem", "Storage Engine Query")]
    [Trait("Class", "QueryExecutive")]
    public class QueryExecutive_should : StorageEngineTestFixture
    {
        [Fact(DisplayName = nameof(QueryExecutive_should) + "_" + nameof(create_master_database_with_default_values))]
        public async Task create_master_database_with_default_values()
        {
            using (var manager = Scope.Resolve<MasterDatabaseDevice>())
            {
                manager.InitialiseDeviceLifetimeScope(Scope);

                var executive = new QueryExecutive(manager);

                var batch = new StringBuilder();
                batch.AppendLine("create database master");

                manager.BeginTransaction(TimeSpan.FromMinutes(15));
                var compiledBatch = executive.CompileBatch(batch.ToString());

                var context = new QueryExecutionContext(manager);
                await compiledBatch(context).ConfigureAwait(true);

                await manager.OpenAsync(true).ConfigureAwait(true);
                await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);
                await manager.CloseAsync().ConfigureAwait(true);
            }
        }

        [Fact(DisplayName = nameof(QueryExecutive_should) + "_" + nameof(create_master_database_with_standard_settings))]
        public async Task create_master_database_with_standard_settings()
        {
            using (var tracker = new TempFileTracker())
            {
                using (var manager = Scope.Resolve<MasterDatabaseDevice>())
                {
                    manager.InitialiseDeviceLifetimeScope(Scope);

                    var executive = new QueryExecutive(manager);

                    var dataFile = tracker.Get("master.mddf");
                    var logFile = tracker.Get("master_log.mlf");

                    var batch = new StringBuilder();

                    manager.BeginTransaction();
                    batch.AppendLine("create database master ");
                    batch.AppendLine("on ");
                    batch.AppendLine("primary ");
                    batch.AppendLine("( ");
                    batch.AppendLine("name=master,");
                    batch.AppendFormat("filename=\'{0}\',", dataFile);
                    batch.AppendLine("size=1024KB");
                    batch.AppendLine(") ");
                    batch.AppendLine("log on ");
                    batch.AppendLine("( ");
                    batch.AppendLine("name=master_log,");
                    batch.AppendFormat("filename=\'{0}\',\n", logFile);
                    batch.AppendLine("size=1024KB");
                    batch.AppendLine(")");
                    var compiledBatch = executive.CompileBatch(batch.ToString());
                    
                    var context = new QueryExecutionContext(manager);
                    await compiledBatch(context).ConfigureAwait(true);
                    await TrunkTransactionContext.CommitAsync().ConfigureAwait(true);

                    //batch.Clear();

                    //manager.BeginTransaction();
                    //batch.AppendLine("create table testtable");
                    //batch.AppendLine("\t(");
                    //batch.AppendLine("\tid int not null identity(1,1),");
                    //batch.AppendLine("\tblit nvarchar(128) not null,");
                    //batch.AppendLine("\tdesc nvarchar(1000) null");
                    //batch.AppendLine("\t)");
                    //batch.AppendLine("\ton default");
                    //batch.AppendLine("go");
                    //await executive.Execute(batch.ToString());
                    //await TrunkTransactionContext.Commit();

                    await manager.CloseAsync().ConfigureAwait(true);
                }
            }
        }

        //public async Task UseDatabaseTest()
        //{
        //}

        [Theory(DisplayName = nameof(QueryExecutive_should) + "_" + nameof(correctly_set_isolation_level_on_context))]
        [InlineData("READ UNCOMMITTED", IsolationLevel.ReadUncommitted)]
        [InlineData("READ COMMITTED", IsolationLevel.ReadCommitted)]
        [InlineData("REPEATABLE READ", IsolationLevel.RepeatableRead)]
        [InlineData("SNAPSHOT", IsolationLevel.Snapshot)]
        [InlineData("SERIALIZABLE", IsolationLevel.Serializable)]
        public async Task correctly_set_isolation_level_on_context(
            string requestedIsolationLevel, IsolationLevel expectedIsolationLevel)
        {
            using (var manager = new MasterDatabaseDevice())
            {
                manager.InitialiseDeviceLifetimeScope(Scope);
                var executive = new QueryExecutive(manager);

                var batch = new StringBuilder();
                batch.AppendLine(
                    $"SET TRANSACTION ISOLATION LEVEL {requestedIsolationLevel};");

                var context = new QueryExecutionContext(manager);
                var compiledBatch = executive.CompileBatch(batch.ToString());
                await compiledBatch(context).ConfigureAwait(true);

                context.IsolationLevel.Should().Be(expectedIsolationLevel);
            }
        }

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);
            builder.RegisterType<MasterDatabaseDevice>()
                .SingleInstance()
                .AsSelf();

            // Use the tracker to determine base configuration
            var temp = GlobalTracker.Get("foo.bar");
            var testFolder = Path.GetDirectoryName(temp);
            var defaultDataFilePath = Path.Combine(testFolder, "Data");
            var defaultLogFilePath = Path.Combine(testFolder, "Log");
            Directory.CreateDirectory(defaultDataFilePath);
            Directory.CreateDirectory(defaultLogFilePath);
        }
    }
}