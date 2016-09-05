using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents a Transaction Identifier.
    /// </summary>
    [Serializable]
    public struct TransactionId : IComparable, ICloneable
    {
        #region Public Fields
        public static readonly TransactionId Zero = new TransactionId(0);
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionId"/> struct.
        /// </summary>
        /// <param name="transactionId">The transaction id.</param>
        public TransactionId(uint transactionId)
        {
            Value = transactionId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the transaction identifier.
        /// </summary>
        public uint Value { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"TransactionId[{Value:X8}]";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is TransactionId)
            {
                var rhs = (TransactionId)obj;
                if (Value == rhs.Value)
                {
                    equal = true;
                }
            }
            return equal;
        }

        /// <summary>
        /// Overridden. Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // Use the virtual page hash code...
            return Value.GetHashCode();
        }

        /// <summary>
        /// Gets a value indicating the relative sort order when
        /// compared against another device page ID.
        /// </summary>
        /// <param name="obj">Object to be compared against.</param>
        /// <returns>
        /// <b>&lt;0</b> this object sorts lower than obj.
        /// <b>=0</b> this object sorts the same as obj.
        /// <b>&gt;0</b> this object sorts higher than obj.
        /// </returns>
        public int CompareTo(TransactionId obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(TransactionId left, TransactionId right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(TransactionId left, TransactionId right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(TransactionId left, TransactionId right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(TransactionId left, TransactionId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is TransactionId)
            {
                order = CompareTo((TransactionId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public TransactionId Clone()
        {
            return (TransactionId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion
    }
}