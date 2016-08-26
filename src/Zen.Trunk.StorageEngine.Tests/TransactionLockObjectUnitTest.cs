using System;
using Xunit;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
	[Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Transaction Lock Object")]
	public class TransactionLockObjectUnitTest
	{
		/// <summary>
		/// Tests the acquiring, release and escalation of DataLock instance
		/// between two simulated transactions.
		/// </summary>
		[Fact(DisplayName = "")]
		public void TransactionDataLockTest()
		{
			// Setup lock hierarchy
			var databaseLock = new DatabaseLock();
			databaseLock.Id = "DBL:01";
			databaseLock.Initialise();

			var objectLock = new ObjectLock();
			objectLock.Id = "OBL:01";
			objectLock.Parent = databaseLock;
			objectLock.Initialise();

			var dataLock = new DataLock();
			dataLock.Id = "DL:01";
			dataLock.Parent = objectLock;
			dataLock.Initialise();

			// Now we spoof transactions so we can test this madness
		    var firstTransactionId = new TransactionId(5);
            var secondTransactionId = new TransactionId(6);

			dataLock.Lock(firstTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(firstTransactionId, DataLockType.Shared));
			dataLock.Lock(firstTransactionId, DataLockType.Update, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(firstTransactionId, DataLockType.Update));
			dataLock.Lock(firstTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(firstTransactionId, DataLockType.Exclusive));

			dataLock.Unlock(firstTransactionId, DataLockType.None);
			Assert.False(dataLock.HasLock(firstTransactionId, DataLockType.None));

			dataLock.Lock(firstTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(firstTransactionId, DataLockType.Shared));
			dataLock.Lock(secondTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(secondTransactionId, DataLockType.Shared));

			dataLock.Lock(secondTransactionId, DataLockType.Update, TimeSpan.FromSeconds(5));
			Assert.True(dataLock.HasLock(secondTransactionId, DataLockType.Update));
			try
			{
				dataLock.Lock(firstTransactionId, DataLockType.Update, TimeSpan.FromSeconds(5));
				Assert.True(false, "First transaction should not be able to acquire update lock.");
			}
			catch (TimeoutException)
			{
			}

			try
			{
				dataLock.Lock(secondTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(5));
				Assert.True(false, "Second transaction should not be able to acquire exclusive lock.");
			}
			catch (TimeoutException)
			{
			}

			dataLock.Unlock(firstTransactionId, DataLockType.None);
			Assert.False(dataLock.HasLock(firstTransactionId, DataLockType.None));

			try
			{
				dataLock.Lock(secondTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(5));
				Assert.True(dataLock.HasLock(secondTransactionId, DataLockType.Exclusive));
			}
			catch (TimeoutException)
			{
				Assert.True(false, "Failed to acquire exclusive lock.");
			}
		}

		/// <summary>
		/// Tests the acquiring, release and escalation of DataLock instance
		/// and ObjectLock between two simulated transactions.
		/// </summary>
		[Fact(DisplayName = "")]
		public void TransactionObjectLockTest()
		{
			// Setup lock hierarchy
			var databaseLock = new DatabaseLock();
			databaseLock.Id = "DBL:01";
			databaseLock.Initialise();

			var objectLock = new ObjectLock();
			objectLock.Id = "OBL:01";
			objectLock.Parent = databaseLock;
			objectLock.Initialise();

			var dataLock = new DataLock();
			dataLock.Id = "DL:01";
			dataLock.Parent = objectLock;
			dataLock.Initialise();

            // Now we spoof transactions so we can test this madness
            var firstTransactionId = new TransactionId(5);
            var secondTransactionId = new TransactionId(6);

            // Acquire shared database locks for both transactions
            databaseLock.Lock(firstTransactionId, DatabaseLockType.Shared, TimeSpan.FromSeconds(30));
			Assert.True(databaseLock.HasLock(firstTransactionId, DatabaseLockType.Shared), "Transaction #1 cannot gain shared database lock.");
			databaseLock.Lock(secondTransactionId, DatabaseLockType.Shared, TimeSpan.FromSeconds(30));
			Assert.True(databaseLock.HasLock(secondTransactionId, DatabaseLockType.Shared), "Transaction #2 cannot gain shared database lock.");

			// Acquire shared object lock and escalate to exclusive
			objectLock.Lock(firstTransactionId, ObjectLockType.Exclusive, TimeSpan.FromSeconds(30));
			Assert.True(objectLock.HasLock(firstTransactionId, ObjectLockType.Exclusive), "Transaction #1 cannot gain exclusive object lock.");

			try
			{
				objectLock.Lock(secondTransactionId, ObjectLockType.Shared, TimeSpan.FromSeconds(5));
				Assert.True(false, "Second transaction should not be able to gain shared object lock.");
			}
			catch
			{
			}

			// First transaction should already have an exclusive lock on data
			dataLock.Lock(firstTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(30));
			Assert.True(dataLock.HasLock(firstTransactionId, DataLockType.Exclusive), "Transaction #1 does not have exclusive data lock.");
		}
	}
}
