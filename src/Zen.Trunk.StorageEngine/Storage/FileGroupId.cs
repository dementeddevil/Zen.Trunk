using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents an File Group Identifier.
    /// </summary>
    [Serializable]
    public struct FileGroupId : IComparable, ICloneable
    {
        #region Public Fields
        public static readonly FileGroupId Invalid = new FileGroupId(0);
        public static readonly FileGroupId Master = new FileGroupId(1);
        public static readonly FileGroupId Primary = new FileGroupId(2);
        #endregion

        #region Private Fields
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupId"/> struct.
        /// </summary>
        /// <param name="fileGroupId">The file-group id.</param>
        [CLSCompliant(false)]
        public FileGroupId(byte fileGroupId)
        {
            Value = fileGroupId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the logical page ID.
        /// </summary>
        [CLSCompliant(false)]
        public byte Value { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"FileGroupId{Value:X8}";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is FileGroupId)
            {
                var rhs = (FileGroupId)obj;
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
        public int CompareTo(FileGroupId obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(FileGroupId left, FileGroupId right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(FileGroupId left, FileGroupId right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(FileGroupId left, FileGroupId right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(FileGroupId left, FileGroupId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is FileGroupId)
            {
                order = ((FileGroupId)this).CompareTo((FileGroupId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public FileGroupId Clone()
        {
            return (FileGroupId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return ((FileGroupId)this).Clone();
        }
        #endregion
    }
}