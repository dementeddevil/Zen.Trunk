﻿using Autofac;
using Zen.Trunk.Storage;

namespace Zen.Trunk.StorageEngine.Tests
{
	using System;
	using System.ComponentModel.Design;
	using System.IO;
	using System.Transactions;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Zen.Trunk.Storage.Locking;
	using Zen.Trunk.Storage.Log;

	[TestClass]
	public class TransactionLockBlockUnitTest
	{
		private static TestContext _testContext;

		[ClassInitialize]
		public static void ClassInitialize(TestContext testContext)
		{
			_testContext = testContext;
		}

		/// <summary>
		/// Test transaction escalation with lock owner blocks.
		/// </summary>
		[TestMethod]
		[TestCategory("Storage Engine: Lock Owner Block")]
		[ExpectedException(typeof(TimeoutException))]
		public void LockOwnerBlockTryGetExclusiveTest()
		{
			// Setup minimal service container we need to get trunk transactions to work
			var container = CreateLockingServiceProvider();
			var dlm = container.Resolve<IDatabaseLockManager>();

			// Create two transaction objects
			ITrunkTransaction firstTransaction = new TrunkTransaction(container, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
			ITrunkTransaction secondTransaction = new TrunkTransaction(container, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
			Assert.AreNotSame(firstTransaction.TransactionId, secondTransaction.TransactionId, "Transaction objects should have different identifiers.");

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

			// Attempt to get exclusive lock on txn 2 (update succeeds but exclusive fails)
			using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
			{
				var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
				dlob.LockItem(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5));
				dlob.LockItem(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5));
			}
		}

		/// <summary>
		/// Test transaction escalation with lock owner blocks.
		/// </summary>
		[TestMethod]
		[TestCategory("Storage Engine: Lock Owner Block")]
		[ExpectedException(typeof(TimeoutException))]
		public void LockOwnerBlockTryEscalateLock()
		{
			// Setup minimal service container we need to get trunk transactions to work
			var container = CreateLockingServiceProvider();
            var dlm = container.Resolve<IDatabaseLockManager>();

            // Create two transaction objects
            ITrunkTransaction firstTransaction = new TrunkTransaction(container, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
			ITrunkTransaction secondTransaction = new TrunkTransaction(container, IsolationLevel.ReadCommitted, TimeSpan.FromSeconds(10));
			Assert.AreNotSame(firstTransaction.TransactionId, secondTransaction.TransactionId, "Transaction objects should have different identifiers.");

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
					Assert.IsTrue(dlob.HasItemLock(new LogicalPageId(logicalId), DataLockType.Shared), string.Format("First transaction should have shared lock on logical page {0}", logicalId));
				}

				dlob.LockItem(new LogicalPageId(5), DataLockType.Shared, TimeSpan.FromSeconds(5));
				Assert.IsTrue(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Shared), "First transaction should have shared lock on logical page 1");
			}

			// Lock for shared read on txn 2
			using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
			{
				var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
				dlob.LockItem(new LogicalPageId(1), DataLockType.Shared, TimeSpan.FromSeconds(5));
				Assert.IsTrue(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Shared), "Second transaction should have shared lock on logical page 1");
			}

			// Attempt to get exclusive lock on txn 2 (both update and exclusive fails)
			//	both fail because the original lock on txn 1 was escalated to a full object lock
			//	the update lock would succeed only if the original locks on txn 1 did not cause
			//	an object-level escalation.
			using (var disp = TrunkTransactionContext.SwitchTransactionContext(secondTransaction))
			{
				var dlob = secondTransactionLOB.GetOrCreateDataLockOwnerBlock(new ObjectId(1), 5);
				dlob.LockItem(new LogicalPageId(1), DataLockType.Update, TimeSpan.FromSeconds(5));
				Assert.IsTrue(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Update), "Second transaction should have update lock on logical page 1");

				dlob.LockItem(new LogicalPageId(1), DataLockType.Exclusive, TimeSpan.FromSeconds(5));
				Assert.IsTrue(dlob.HasItemLock(new LogicalPageId(1), DataLockType.Exclusive), "Second transaction should not have exclusive lock on logical page 1");
			}
		}

		private void VerifyDataLockHeld(IDatabaseLockManager dlm, uint objectId, ulong logicalId, DataLockType lockType, string message)
		{
			var lockObject = dlm.GetDataLock(new ObjectId(objectId), new LogicalPageId(logicalId));
			try
			{
				Assert.IsTrue(lockObject.HasLock(lockType), message);
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
				Assert.IsFalse(lockObject.HasLock(lockType), message);
			}
			finally
			{
				lockObject.ReleaseRefLock();
			}
		}

		private ILifetimeScope CreateLockingServiceProvider()
		{
            var builder = new StorageEngineBuilder()
                .WithGlobalLockManager()
                .WithDatabaseLockManager(new DatabaseId(1));

			// ** Master log page device is needed to getting the atomic transaction id
			// TODO: Change MasterLogPageDevice to use interface so we can mock
			//	and implement the single method call we need...
			var pathName = Path.Combine(_testContext.TestDir, "LogDevice.mlb");
		    builder.RegisterType<MasterLogPageDevice>()
		        .WithParameter("pathName", pathName)
		        .AsSelf()
		        .SingleInstance();

            return builder.Build();
		}
	}
}
