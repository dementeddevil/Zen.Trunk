using System;
using System.Threading.Tasks;
using Xunit;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    [Trait("Subsystem", "Storage Engine")]
    [Trait("Class", "Transaction Lock Object")]
    public class TransactionLockObjectUnitTest : IClassFixture<StorageEngineTestFixture>
    {
        /// <summary>
        /// Tests the acquiring, release and escalation of DataLock instance
        /// between two simulated transactions.
        /// </summary>
        [Fact(DisplayName = "Test acquiring, release and escalation of DataLock between two transactions.")]
        public async Task TransactionDataLockTest()
        {
            // Setup lock hierarchy
            var builder = new TransactionLockHierarchyBuilder();
            builder.WithRootLock()
                .WithDatabaseLock("DBL:01")
                .WithObjectLock("OBL:01")
                .WithDataLock("DL:01");
            ITransactionLockHierarchy lockHierarchy;
            using (lockHierarchy = builder.Build())
            {
                var dataLock = lockHierarchy.DataLocks["DL:01"];

                // Now we spoof transactions so we can test this madness
                var firstLockOwner = new LockOwnerIdentity(
                    SessionId.Zero, new TransactionId(5));
                var secondLockOwner = new LockOwnerIdentity(
                    SessionId.Zero, new TransactionId(6));

                await dataLock
                    .LockAsync(firstLockOwner, DataLockType.Shared, TimeSpan.FromSeconds(30))
                    .ConfigureAwait(true);
                Assert.True(await dataLock
                    .HasLockAsync(firstLockOwner, DataLockType.Shared)
                    .ConfigureAwait(true));

                await dataLock
                    .LockAsync(firstLockOwner, DataLockType.Update, TimeSpan.FromSeconds(30))
                    .ConfigureAwait(true);
                Assert.True(await dataLock
                    .HasLockAsync(firstLockOwner, DataLockType.Update)
                    .ConfigureAwait(true));

                await dataLock
                    .LockAsync(firstLockOwner, DataLockType.Exclusive, TimeSpan.FromSeconds(30))
                    .ConfigureAwait(true);
                Assert.True(await dataLock
                    .HasLockAsync(firstLockOwner, DataLockType.Exclusive)
                    .ConfigureAwait(true));

                await dataLock
                    .UnlockAsync(firstLockOwner, DataLockType.None)
                    .ConfigureAwait(true);
                Assert.False(await dataLock
                    .HasLockAsync(firstLockOwner, DataLockType.None)
                    .ConfigureAwait(true));

                await dataLock
                    .LockAsync(firstLockOwner, DataLockType.Shared, TimeSpan.FromSeconds(30))
                    .ConfigureAwait(true);
                Assert.True(await dataLock
                    .HasLockAsync(firstLockOwner, DataLockType.Shared)
                    .ConfigureAwait(true));

                await dataLock
                    .LockAsync(secondLockOwner, DataLockType.Shared, TimeSpan.FromSeconds(30))
                    .ConfigureAwait(true);
                Assert.True(await dataLock
                    .HasLockAsync(secondLockOwner, DataLockType.Shared)
                    .ConfigureAwait(true));

                await dataLock.LockAsync(secondLockOwner, DataLockType.Update, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                Assert.True(await dataLock.HasLockAsync(secondLockOwner, DataLockType.Update).ConfigureAwait(true));
                await Assert.ThrowsAsync<LockTimeoutException>(
                    async () =>
                    {
                        await dataLock
                            .LockAsync(firstLockOwner, DataLockType.Update, TimeSpan.FromSeconds(5))
                            .ConfigureAwait(true);
                    });

                await Assert.ThrowsAsync<LockTimeoutException>(
                    async () =>
                    {
                        await dataLock
                            .LockAsync(secondLockOwner, DataLockType.Exclusive, TimeSpan.FromSeconds(5))
                            .ConfigureAwait(true);
                    });

                await dataLock.UnlockAsync(firstLockOwner, DataLockType.None).ConfigureAwait(true);
                Assert.False(await dataLock.HasLockAsync(firstLockOwner, DataLockType.None).ConfigureAwait(true));

                await dataLock.LockAsync(secondLockOwner, DataLockType.Exclusive, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                Assert.True(await dataLock.HasLockAsync(secondLockOwner, DataLockType.Exclusive).ConfigureAwait(true));

                await dataLock.UnlockAsync(secondLockOwner, DataLockType.None).ConfigureAwait(true);
                Assert.False(await dataLock.HasLockAsync(secondLockOwner, DataLockType.None).ConfigureAwait(true));
            }

            Assert.Equal(
                lockHierarchy.ExpectedFinalReleaseCount,
                lockHierarchy.ActualFinalReleaseCount);
        }

        /// <summary>
        /// Tests the acquiring, release and escalation of DataLock instance
        /// and ObjectLock between two simulated transactions.
        /// </summary>
        [Fact(DisplayName = "Test acquiring, release and escalation of DataLock and ObjectLock between two transactions.")]
        public async Task TransactionObjectLockTest()
        {
            // Setup lock hierarchy
            var builder = new TransactionLockHierarchyBuilder();
            builder.WithRootLock()
                .WithDatabaseLock("DBL:01")
                .WithObjectLock("OBL:01")
                .WithDataLock("DL:01");
            ITransactionLockHierarchy lockHierarchy;
            using (lockHierarchy = builder.Build())
            {
                var databaseLock = lockHierarchy.DatabaseLocks["DBL:01"];
                var objectLock = lockHierarchy.ObjectLocks["OBL:01"];
                var dataLock = lockHierarchy.DataLocks["DL:01"];

                // Now we spoof transactions so we can test this madness
                var firstLockOwner = new LockOwnerIdentity(
                    SessionId.Zero,
                    new TransactionId(5));
                var secondLockOwner = new LockOwnerIdentity(
                    SessionId.Zero,
                    new TransactionId(6));

                // Acquire shared database locks for both transactions
                await databaseLock.LockAsync(firstLockOwner, DatabaseLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
                Assert.True(
                    await databaseLock.HasLockAsync(firstLockOwner, DatabaseLockType.Shared).ConfigureAwait(true),
                    "Transaction #1 cannot gain shared database lock.");
                await databaseLock.LockAsync(secondLockOwner, DatabaseLockType.Shared, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
                Assert.True(
                    await databaseLock.HasLockAsync(secondLockOwner, DatabaseLockType.Shared).ConfigureAwait(true),
                    "Transaction #2 cannot gain shared database lock.");

                // Acquire shared object lock and escalate to exclusive
                await objectLock.LockAsync(firstLockOwner, ObjectLockType.Exclusive, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
                Assert.True(
                    await objectLock.HasLockAsync(firstLockOwner, ObjectLockType.Exclusive).ConfigureAwait(true),
                    "Transaction #1 cannot gain exclusive object lock.");

                try
                {
                    await objectLock.LockAsync(secondLockOwner, ObjectLockType.Shared, TimeSpan.FromSeconds(5)).ConfigureAwait(true);
                    Assert.True(false, "Second transaction should not be able to gain shared object lock.");
                }
                catch
                {
                    // ignored
                }

                // First transaction should already have an exclusive lock on data
                await dataLock.LockAsync(firstLockOwner, DataLockType.Exclusive, TimeSpan.FromSeconds(30)).ConfigureAwait(true);
                Assert.True(
                    await dataLock.HasLockAsync(firstLockOwner, DataLockType.Exclusive).ConfigureAwait(true),
                    "Transaction #1 does not have exclusive data lock.");

                await dataLock.UnlockAsync(firstLockOwner, DataLockType.None).ConfigureAwait(true);
                Assert.False(await dataLock.HasLockAsync(firstLockOwner, DataLockType.None).ConfigureAwait(true));

                await objectLock.UnlockAsync(firstLockOwner, ObjectLockType.None).ConfigureAwait(true);
                Assert.False(await objectLock.HasLockAsync(firstLockOwner, ObjectLockType.None).ConfigureAwait(true));

                await databaseLock.UnlockAsync(firstLockOwner, DatabaseLockType.None).ConfigureAwait(true);
                Assert.False(await databaseLock.HasLockAsync(firstLockOwner, DatabaseLockType.None).ConfigureAwait(true));

                await databaseLock.UnlockAsync(secondLockOwner, DatabaseLockType.None).ConfigureAwait(true);
                Assert.False(await databaseLock.HasLockAsync(secondLockOwner, DatabaseLockType.None).ConfigureAwait(true));
            }

            Assert.Equal(
                lockHierarchy.ExpectedFinalReleaseCount,
                lockHierarchy.ActualFinalReleaseCount);
        }
    }
}
