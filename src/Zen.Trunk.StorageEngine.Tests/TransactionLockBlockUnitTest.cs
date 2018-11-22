using System;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Transaction Lock Block")]
    public class TransactionLockBlockUnitTest : IClassFixture<StorageEngineTestFixture>, IDisposable
    {
        private readonly StorageEngineTestFixture _fixture;
        private readonly ILifetimeScope _scope;

        public TransactionLockBlockUnitTest(StorageEngineTestFixture fixture)
        {
            _fixture = fixture;
            _scope = _fixture.Scope.BeginLifetimeScope(
                builder =>
                {
                    // ** Master log page device is needed to getting the atomic transaction id
                    // TODO: Change MasterLogPageDevice to use interface so we can mock
                    //	and implement the single method call we need...
                    var pathName = _fixture.GlobalTracker.Get("LogDevice.mlb");
                    builder.RegisterType<MasterLogPageDevice>()
                        .WithParameter("pathName", pathName)
                        .As<IMasterLogPageDevice>()
                        .SingleInstance();
                });
        }

        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName =
            @"Given two transactions holding shared locks to the same resource
When one transaction upgrades to an update lock the operation succeeds but when trying for an exclusive lock
Then the attempt to gain an exclusive lock fails.")]
        public async Task LockOwnerBlockTryGetExclusiveTest()
        {
            // Setup minimal service container we need to get trunk transactions to work
            var dlm = _scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLob = ((ITrunkTransactionPrivate)firstTransaction).GetTransactionLockOwnerBlock(dlm);
            var secondTransactionLob = ((ITrunkTransactionPrivate)secondTransaction).GetTransactionLockOwnerBlock(dlm);

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dlob = firstTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            }

            // Lock for shared read on txn 2
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            }

            await Assert
                .ThrowsAsync<TimeoutException>(
                    async () =>
                    {
                        // Attempt to get exclusive lock on txn 2 (update succeeds but exclusive fails)
                        using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
                        {
                            var dlob = secondTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                            await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                            await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                        }
                    })
                .ConfigureAwait(true);
        }

        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName = "Verify no page lock escalation occurs on txn 2 once shared page lock has escalated to object lock on txn 1")]
        public async Task LockOwnerBlockTryEscalateLock()
        {
            // Setup minimal service container we need to get trunk transactions to work
            var dlm = _scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLob = ((ITrunkTransactionPrivate)firstTransaction).GetTransactionLockOwnerBlock(dlm);
            var secondTransactionLob = ((ITrunkTransactionPrivate)secondTransaction).GetTransactionLockOwnerBlock(dlm);

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dlob = firstTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                for (ulong logicalId = 0; logicalId < 5; ++logicalId)
                {
                    await dlob.LockItemAsync(new LogicalPageId(logicalId), DataLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                    Assert.True(
                        await dlob.HasItemLockAsync(new LogicalPageId(logicalId), DataLockType.Shared).ConfigureAwait(true),
                        $"First transaction should have shared lock on logical page {logicalId}");
                }

                await dlob.LockItemAsync(new LogicalPageId(5), DataLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                Assert.True(
                    await dlob.HasItemLockAsync(new LogicalPageId(1), DataLockType.Shared).ConfigureAwait(true),
                    "First transaction should have shared lock on logical page 1");
            }

            // Lock for shared read on txn 2
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                Assert.True(
                    await dlob.HasItemLockAsync(new LogicalPageId(1), DataLockType.Shared).ConfigureAwait(true),
                    "Second transaction should have shared lock on logical page 1");
            }

            // Attempt to get exclusive lock on txn 2 (both update and exclusive fails)
            //	both fail because the original lock on txn 1 was escalated to a full object lock
            //	the update lock would succeed only if the original locks on txn 1 did not cause
            //	an object-level escalation.
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLob.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                await Assert
                    .ThrowsAsync<TimeoutException>(
                        async () =>
                        {
                            await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                            Assert.True(
                                await dlob.HasItemLockAsync(new LogicalPageId(1), DataLockType.Update).ConfigureAwait(true),
                                "Second transaction should have update lock on logical page 1");
                        })
                    .ConfigureAwait(true);
                await Assert
                    .ThrowsAsync<TimeoutException>(
                        async () =>
                        {
                            await dlob.LockItemAsync(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                            Assert.True(
                                await dlob.HasItemLockAsync(new LogicalPageId(1), DataLockType.Exclusive).ConfigureAwait(true),
                                "Second transaction should not have exclusive lock on logical page 1");
                        })
                    .ConfigureAwait(true);
            }
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
