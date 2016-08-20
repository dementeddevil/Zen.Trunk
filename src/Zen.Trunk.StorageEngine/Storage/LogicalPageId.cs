using System;
using System.Globalization;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents a Device Page Identifier.
    /// </summary>
    [Serializable]
    public struct LogicalPageId : IComparable, ICloneable
    {
        #region Private Fields
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicalPageId"/> struct.
        /// </summary>
        /// <param name="logicalPageId">The logical page id.</param>
        [CLSCompliant(false)]
        public LogicalPageId(ulong logicalPageId)
        {
            Value = logicalPageId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the logical page ID.
        /// </summary>
        [CLSCompliant(false)]
        public ulong Value { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"LogicalPageId{Value:X8}";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            bool equal = false;
            if (obj is LogicalPageId)
            {
                LogicalPageId rhs = (LogicalPageId)obj;
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
        public int CompareTo(LogicalPageId obj)
        {
            int order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(LogicalPageId left, LogicalPageId right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(LogicalPageId left, LogicalPageId right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(LogicalPageId left, LogicalPageId right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(LogicalPageId left, LogicalPageId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            int order = -1;
            if (obj is LogicalPageId)
            {
                order = ((LogicalPageId)this).CompareTo((LogicalPageId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public LogicalPageId Clone()
        {
            return (LogicalPageId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return ((LogicalPageId)this).Clone();
        }
        #endregion
    }
}
