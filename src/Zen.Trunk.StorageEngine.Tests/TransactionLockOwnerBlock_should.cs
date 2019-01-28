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
    // ReSharper disable once InconsistentNaming
    public class TransactionLockOwnerBlock_should : IClassFixture<StorageEngineTestFixture>, IDisposable
    {
        private readonly ILifetimeScope _scope;

        public TransactionLockOwnerBlock_should(StorageEngineTestFixture fixture)
        {
            _scope = fixture.Scope.BeginLifetimeScope(
                builder =>
                {
                    // ** Master log page device is needed to getting the atomic transaction id
                    // TODO: Change MasterLogPageDevice to use interface so we can mock
                    //	and implement the single method call we need...
                    var pathName = fixture.GlobalTracker.Get("LogDevice.mlb");
                    builder.RegisterType<MasterLogPageDevice>()
                        .WithParameter("pathName", pathName)
                        .As<IMasterLogPageDevice>()
                        .SingleInstance();
                });
        }

        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName = nameof(TransactionLockOwnerBlock_should) + "_" + nameof(deny_access_to_exclusive_lock_while_shared_lock_held))]
        public async Task deny_access_to_exclusive_lock_while_shared_lock_held()
        {
            var objectId = new ObjectId(1);
            var startLogicalId = new LogicalPageId(10);

            // Setup minimal service container we need to get trunk transactions to work
            var dlm = _scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLob = firstTransaction.GetTransactionLockOwnerBlock(dlm);
            var secondTransactionLob = secondTransaction.GetTransactionLockOwnerBlock(dlm);

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dataLockOwnerBlock = firstTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                await dataLockOwnerBlock
                    .LockItemAsync(startLogicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);
            }

            // Lock for shared read on txn 2
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dataLockOwnerBlock = secondTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                await dataLockOwnerBlock
                    .LockItemAsync(startLogicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);
            }

            await Assert
                .ThrowsAsync<LockTimeoutException>(
                    async () =>
                    {
                        // Attempt to get exclusive lock on txn 2 (update succeeds but exclusive fails)
                        using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
                        {
                            var dataLockOwnerBlock = secondTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                            await dataLockOwnerBlock
                                .LockItemAsync(startLogicalId, DataLockType.Update, TimeSpan.FromSeconds(5))
                                .ConfigureAwait(true);
                            await dataLockOwnerBlock
                                .LockItemAsync(startLogicalId, DataLockType.Exclusive, TimeSpan.FromSeconds(5))
                                .ConfigureAwait(true);
                        }
                    })
                .ConfigureAwait(true);
        }

        /// <summary>
        /// Verify lock escalation occurs when number of data locks are exceeded
        /// </summary>
        /// <returns></returns>
        [Fact(DisplayName = nameof(TransactionLockOwnerBlock_should) + "_" + nameof(verify_lock_escalation_occurs_when_granular_lock_limit_exceeded))]
        public async Task verify_lock_escalation_occurs_when_granular_lock_limit_exceeded()
        {
            var objectId = new ObjectId(2);
            var startLogicalId = new LogicalPageId(20);
            var endLogicalId = new LogicalPageId(25);

            // Setup minimal service container we need to get trunk transactions to work
            var dlm = _scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLob = firstTransaction.GetTransactionLockOwnerBlock(dlm);

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dataLockOwnerBlock = firstTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                for (var logicalId = startLogicalId; logicalId < endLogicalId; logicalId = logicalId.Next)
                {
                    await dataLockOwnerBlock
                        .LockItemAsync(logicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                        .ConfigureAwait(true);
                    Assert.True(
                        await dataLockOwnerBlock
                            .HasItemLockAsync(logicalId, DataLockType.Shared)
                            .ConfigureAwait(true),
                        $"First transaction should have shared lock on logical page {logicalId}");
                }

                // Should not have full shared lock
                var hasOwnerLock = await dataLockOwnerBlock
                    .HasOwnerLockAsync(ObjectLockType.Shared)
                    .ConfigureAwait(true);
                Assert.False(hasOwnerLock);

                // Should have an intent shared lock
                hasOwnerLock = await dataLockOwnerBlock
                    .HasOwnerLockAsync(ObjectLockType.IntentShared)
                    .ConfigureAwait(true);
                Assert.True(hasOwnerLock);

                await dataLockOwnerBlock
                    .LockItemAsync(endLogicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);

                // Escalation should have occurred such that we have a shared lock on the owner
                hasOwnerLock = await dataLockOwnerBlock
                    .HasOwnerLockAsync(ObjectLockType.Shared)
                    .ConfigureAwait(true);
                Assert.True(hasOwnerLock);
            }
        }

        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName = nameof(TransactionLockOwnerBlock_should) + "_" + nameof(verify_no_escalation_on_txn2_when_escalated_on_txn1))]
        public async Task verify_no_escalation_on_txn2_when_escalated_on_txn1()
        {
            var objectId = new ObjectId(3);
            var startLogicalId = new LogicalPageId(30);
            var endLogicalId = new LogicalPageId(35);

            // Setup minimal service container we need to get trunk transactions to work
            var dlm = _scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(_scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLob = firstTransaction.GetTransactionLockOwnerBlock(dlm);
            var secondTransactionLob = secondTransaction.GetTransactionLockOwnerBlock(dlm);

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dataLockOwnerBlock = firstTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                for (var logicalId = startLogicalId; logicalId < endLogicalId; logicalId = logicalId.Next)
                {
                    await dataLockOwnerBlock
                        .LockItemAsync(logicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                        .ConfigureAwait(true);
                    Assert.True(
                        await dataLockOwnerBlock
                            .HasItemLockAsync(logicalId, DataLockType.Shared)
                            .ConfigureAwait(true),
                        $"First transaction should have shared lock on logical page {logicalId}");
                }

                await dataLockOwnerBlock
                    .LockItemAsync(endLogicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);
                Assert.True(
                    await dataLockOwnerBlock
                        .HasItemLockAsync(startLogicalId, DataLockType.Shared)
                        .ConfigureAwait(true),
                    "First transaction should have shared lock on logical page 1");
            }

            // Lock for shared read on txn 2
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dataLockOwnerBlock = secondTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                await dataLockOwnerBlock
                    .LockItemAsync(startLogicalId, DataLockType.Shared, TimeSpan.FromSeconds(5))
                    .ConfigureAwait(true);
                Assert.True(
                    await dataLockOwnerBlock
                        .HasItemLockAsync(startLogicalId, DataLockType.Shared)
                        .ConfigureAwait(true),
                    "Second transaction should have shared lock on logical page 1");
            }

            // Attempt to get exclusive lock on txn 2 (both update and exclusive fails)
            //	both fail because the original lock on txn 1 was escalated to a full object lock
            //	the update lock would succeed only if the original locks on txn 1 did not cause
            //	an object-level escalation.
            using (TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dataLockOwnerBlock = secondTransactionLob.GetOrCreateDataLockOwnerBlock(objectId, 5);
                await Assert
                    .ThrowsAsync<LockTimeoutException>(
                        async () =>
                        {
                            await dataLockOwnerBlock
                                .LockItemAsync(startLogicalId, DataLockType.Update, TimeSpan.FromSeconds(5))
                                .ConfigureAwait(true);
                            Assert.True(
                                await dataLockOwnerBlock
                                    .HasItemLockAsync(startLogicalId, DataLockType.Update)
                                    .ConfigureAwait(true),
                                "Second transaction should have update lock on logical page 1");
                        })
                    .ConfigureAwait(true);
                await Assert
                    .ThrowsAsync<LockTimeoutException>(
                        async () =>
                        {
                            await dataLockOwnerBlock
                                .LockItemAsync(startLogicalId, DataLockType.Exclusive, TimeSpan.FromSeconds(5))
                                .ConfigureAwait(true);
                            Assert.True(
                                await dataLockOwnerBlock
                                    .HasItemLockAsync(startLogicalId, DataLockType.Exclusive)
                                    .ConfigureAwait(true),
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
