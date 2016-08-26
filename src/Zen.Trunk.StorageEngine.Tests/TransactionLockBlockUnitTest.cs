using System;
using System.IO;
using System.Reflection;
using System.Transactions;
using Autofac;
using Xunit;
using Zen.Trunk.Storage.Locking;
using Zen.Trunk.Storage.Log;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Transaction Lock Block")]
    public class TransactionLockBlockUnitTest : AutofacStorageEngineUnitTests
    {
        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName =
            @"Given two transactions holding shared locks to the same resource
When one transaction upgrades to an update lock the operation succeeds but when trying for an exclusive lock
Then the attempt to gain an exclusive lock fails.")]
        public void LockOwnerBlockTryGetExclusiveTest()
        {
            // Setup minimal service container we need to get trunk transactions to work
            var dlm = Scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(Scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(Scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLOB = ((ITrunkTransactionPrivate)firstTransaction).TransactionLocks;
            var secondTransactionLOB = ((ITrunkTransactionPrivate)secondTransaction).TransactionLocks;

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (var disp = TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dlob = firstTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                dlob.LockItem(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5));
            }

            // Lock for shared read on txn 2
            using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                dlob.LockItem(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5));
            }

            Assert.Throws<TimeoutException>(
                () =>
                {
                    // Attempt to get exclusive lock on txn 2 (update succeeds but exclusive fails)
                    using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
                    {
                        var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                        dlob.LockItem(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5));
                        dlob.LockItem(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5));
                    }
                });
        }

        /// <summary>
        /// Test transaction escalation with lock owner blocks.
        /// </summary>
        [Fact(DisplayName = "Verify no page lock escalation occurs on txn 2 once shared page lock has escalated to object lock on txn 1")]
        public void LockOwnerBlockTryEscalateLock()
        {
            // Setup minimal service container we need to get trunk transactions to work
            var dlm = Scope.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(Scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            ITrunkTransaction secondTransaction = new TrunkTransaction(Scope, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
            Assert.NotEqual(firstTransaction.TransactionId, secondTransaction.TransactionId);

            // We need access to the Lock Owner Block (LOB) for each transaction
            var firstTransactionLOB = ((ITrunkTransactionPrivate)firstTransaction).TransactionLocks;
            var secondTransactionLOB = ((ITrunkTransactionPrivate)secondTransaction).TransactionLocks;

            // Locking semantics use transaction id held on current thread each lock/unlock needs scope

            // Lock for shared read on txn 1
            using (var disp = TrunkTransactionContext.SwitchTransactionContext(firstTransaction))
            {
                var dlob = firstTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                for (ulong logicalId = 0; logicalId < 5; ++logicalId)
                {
                    dlob.LockItem(new LogicalPageId(logicalId), DataLockType.Shared, TimeSpan.FromSeconds(5));
                    Assert.True(dlob.HasItemLock(new LogicalPageId(logicalId), DataLockType.Shared), string.Format("First transaction should have shared lock on logical page {0}", logicalId));
                }

                dlob.LockItem(new LogicalPageId(5), DataLockType.Shared, TimeSpan.FromSeconds(5));
                Assert.True(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Shared), "First transaction should have shared lock on logical page 1");
            }

            // Lock for shared read on txn 2
            using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                dlob.LockItem(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5));
                Assert.True(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Shared), "Second transaction should have shared lock on logical page 1");
            }

            // Attempt to get exclusive lock on txn 2 (both update and exclusive fails)
            //	both fail because the original lock on txn 1 was escalated to a full object lock
            //	the update lock would succeed only if the original locks on txn 1 did not cause
            //	an object-level escalation.
            using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
            {
                var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
                Assert.Throws<TimeoutException>(
                    () =>
                    {
                        dlob.LockItem(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5));
                        Assert.True(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Update), "Second transaction should have update lock on logical page 1");
                    });
                Assert.Throws<TimeoutException>(
                    () =>
                    {
                        dlob.LockItem(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5));
                        Assert.True(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Exclusive), "Second transaction should not have exclusive lock on logical page 1");
                    });
            }
        }

        private void VerifyDataLockHeld(IDatabaseLockManager dlm, uint objectId, ulong logicalId, DataLockType lockType, string message)
        {
            var lockObject = dlm.GetDataLock(new ObjectId(objectId), new LogicalPageId(logicalId));
            try
            {
                Assert.True(lockObject.HasLock(lockType), message);
            }
            finally
            {
                lockObject.ReleaseRefLock();
            }
        }

        private void VerifyDataLockNotHeld(IDatabaseLockManager dlm, uint objectId, ulong logicalId, DataLockType lockType, string message)
        {
            var lockObject = dlm.GetDataLock(new ObjectId(objectId), new LogicalPageId(logicalId));
            try
            {
                Assert.False(lockObject.HasLock(lockType), message);
            }
            finally
            {
                lockObject.ReleaseRefLock();
            }
        }

        protected override void InitializeContainerBuilder(ContainerBuilder builder)
        {
            base.InitializeContainerBuilder(builder);

            // ** Master log page device is needed to getting the atomic transaction id
            // TODO: Change MasterLogPageDevice to use interface so we can mock
            //	and implement the single method call we need...
            var pathName = GlobalTracker.Get("LogDevice.mlb");
            builder.RegisterType<MasterLogPageDevice>()
                .WithParameter("pathName", pathName)
                .AsSelf()
                .SingleInstance();
        }
    }
}
