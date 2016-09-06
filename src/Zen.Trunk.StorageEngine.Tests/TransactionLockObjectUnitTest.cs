using System;
using System.Threading.Tasks;
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
		public async Task TransactionDataLockTest()
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

			await dataLock.LockAsync(firstTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(firstTransactionId, DataLockType.Shared));
			await dataLock.LockAsync(firstTransactionId, DataLockType.Update, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(firstTransactionId, DataLockType.Update).ConfigureAwait(true));
            await dataLock.LockAsync(firstTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(firstTransactionId, DataLockType.Exclusive).ConfigureAwait(true));

            await dataLock.UnlockAsync(firstTransactionId, DataLockType.None).ConfigureAwait(true);
			Assert.False(await dataLock.HasLockAsync(firstTransactionId, DataLockType.None).ConfigureAwait(true));

            await dataLock.LockAsync(firstTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(firstTransactionId, DataLockType.Shared).ConfigureAwait(true));
            await dataLock.LockAsync(secondTransactionId, DataLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(secondTransactionId, DataLockType.Shared).ConfigureAwait(true));

            await dataLock.LockAsync(secondTransactionId, DataLockType.Update, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
			Assert.True(await dataLock.HasLockAsync(secondTransactionId, DataLockType.Update).ConfigureAwait(true));
			try
			{
                await dataLock.LockAsync(firstTransactionId, DataLockType.Update, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
				Assert.True(false, "First transaction should not be able to acquire update lock.");
			}
			catch (TimeoutException)
			{
			}

			try
			{
                await dataLock.LockAsync(secondTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
				Assert.True(false, "Second transaction should not be able to acquire exclusive lock.");
			}
			catch (TimeoutException)
			{
			}

            await dataLock.UnlockAsync(firstTransactionId, DataLockType.None).ConfigureAwait(true);
			Assert.False(await dataLock.HasLockAsync(firstTransactionId, DataLockType.None).ConfigureAwait(true));

			try
			{
                await dataLock.LockAsync(secondTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
				Assert.True(await dataLock.HasLockAsync(secondTransactionId, DataLockType.Exclusive).ConfigureAwait(true));
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
		public async Task TransactionObjectLockTest()
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
            await databaseLock.LockAsync(firstTransactionId, DatabaseLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(
                await databaseLock.HasLockAsync(firstTransactionId, DatabaseLockType.Shared).ConfigureAwait(true),
                "Transaction #1 cannot gain shared database lock.");
            await databaseLock.LockAsync(secondTransactionId, DatabaseLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(
                await databaseLock.HasLockAsync(secondTransactionId, DatabaseLockType.Shared).ConfigureAwait(true),
                "Transaction #2 cannot gain shared database lock.");

            // Acquire shared object lock and escalate to exclusive
            await objectLock.LockAsync(firstTransactionId, ObjectLockType.Exclusive, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(
                await objectLock.HasLockAsync(firstTransactionId, ObjectLockType.Exclusive).ConfigureAwait(true),
                "Transaction #1 cannot gain exclusive object lock.");

			try
			{
                await objectLock.LockAsync(secondTransactionId, ObjectLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
				Assert.True(false, "Second transaction should not be able to gain shared object lock.");
			}
			catch
			{
			}

			// First transaction should already have an exclusive lock on data
			await dataLock.LockAsync(firstTransactionId, DataLockType.Exclusive, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
			Assert.True(
                await dataLock.HasLockAsync(firstTransactionId, DataLockType.Exclusive).ConfigureAwait(true),
                "Transaction #1 does not have exclusive data lock.");
		}
	}
}
