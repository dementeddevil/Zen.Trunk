// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrunkTransactionExtensions.cs" company="Zen Design Software">
//   Copyright © Zen Design Software 2019
// </copyright>
// <summary>
//   Zen.Trunk.NoInstaller.Zen.Trunk.StorageEngine.Tests.TrunkTransactionExtensions.cs
//   Author:   Adrian Lewis
//   Created:  10:28 28/01/2019
//   Updated:  10:28 28/01/2019
// 
//   Summary description
//   (blah)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage
{
    public static class TrunkTransactionExtensions
    {
        internal static TransactionLockOwnerBlock GetTransactionLockOwnerBlock(
            this ITrunkTransaction transaction, IDatabaseLockManager lockManager)
        {
            return ((ITrunkTransactionPrivate) transaction).GetTransactionLockOwnerBlock(lockManager);
        }
    }
}