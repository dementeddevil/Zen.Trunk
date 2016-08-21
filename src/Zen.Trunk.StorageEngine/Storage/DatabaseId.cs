using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents an Database Identifier.
    /// </summary>
    [Serializable]
    public struct DatabaseId : IComparable, ICloneable
    {
        #region Public Fields
        public static readonly DatabaseId Zero = new DatabaseId(0);
        #endregion

        #region Private Fields
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseId"/> struct.
        /// </summary>
        /// <param name="databaseId">The database id.</param>
        [CLSCompliant(false)]
        public DatabaseId(ushort databaseId)
        {
            Value = databaseId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the logical page ID.
        /// </summary>
        [CLSCompliant(false)]
        public ushort Value { get; }

        public DatabaseId Next => new DatabaseId((ushort)(Value + 1));
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"DatabaseId{Value:X4}";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is DatabaseId)
            {
                var rhs = (DatabaseId)obj;
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
        public int CompareTo(DatabaseId obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(DatabaseId left, DatabaseId right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(DatabaseId left, DatabaseId right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(DatabaseId left, DatabaseId right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(DatabaseId left, DatabaseId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is DatabaseId)
            {
                order = ((DatabaseId)this).CompareTo((DatabaseId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public DatabaseId Clone()
        {
            return (DatabaseId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return ((DatabaseId)this).Clone();
        }
        #endregion
    }
}