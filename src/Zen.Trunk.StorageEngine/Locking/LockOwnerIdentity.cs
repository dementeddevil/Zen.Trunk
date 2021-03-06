﻿namespace Zen.Trunk.Storage.Locking
{
    /// <summary>
    /// <c>LockOwnerIdentity</c> defines the owner of a lock object.
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Locking.TransactionLockBase" />
    /// <seealso cref="Zen.Trunk.Storage.Locking.IReferenceLock" />
    public struct LockOwnerIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LockOwnerIdentity"/> struct.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        public LockOwnerIdentity(SessionId sessionId, TransactionId transactionId)
        {
            SessionId = sessionId;
            TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        public SessionId SessionId { get; }

        /// <summary>
        /// Gets the transaction identifier.
        /// </summary>
        /// <value>
        /// The transaction identifier.
        /// </value>
        public TransactionId TransactionId { get; }

        /// <summary>
        /// Gets the session only lock owner.
        /// </summary>
        /// <value>
        /// The session only lock owner.
        /// </value>
        /// <remarks>
        /// The ident object returned only has the session identifier
        /// copied from this instance with the transaction identifier
        /// set to zero.
        /// </remarks>
        public LockOwnerIdentity SessionOnlyLockOwner
        {
            get
            {
                if (TransactionId == TransactionId.Zero)
                {
                    return this;
                }
                return new LockOwnerIdentity(SessionId, TransactionId.Zero);
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var rhs = (LockOwnerIdentity)obj;
            return SessionId == rhs.SessionId && TransactionId == rhs.TransactionId;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return SessionId.GetHashCode() << 5 ^ TransactionId.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"LOI:[{SessionId}:{TransactionId}]";
        }

        public static bool operator ==(LockOwnerIdentity lhs, LockOwnerIdentity rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(LockOwnerIdentity lhs, LockOwnerIdentity rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}