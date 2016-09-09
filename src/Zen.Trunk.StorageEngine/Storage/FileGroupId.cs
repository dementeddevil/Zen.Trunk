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
        /// <summary>
        /// The invalid
        /// </summary>
        public static readonly FileGroupId Invalid = new FileGroupId(0);

        /// <summary>
        /// The master
        /// </summary>
        public static readonly FileGroupId Master = new FileGroupId(1);

        /// <summary>
        /// The primary
        /// </summary>
        public static readonly FileGroupId Primary = new FileGroupId(2);
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="FileGroupId"/> struct.
        /// </summary>
        /// <param name="fileGroupId">The file-group id.</param>
        public FileGroupId(byte fileGroupId)
        {
            Value = fileGroupId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the logical page ID.
        /// </summary>
        public byte Value { get; }

        /// <summary>
        /// Gets a file-group identifier that is logically next in the sequence.
        /// </summary>
        /// <value>
        /// The next.
        /// </value>
        public FileGroupId Next => new FileGroupId((byte)(Value + 1));

        /// <summary>
        /// Gets a value indicating whether this instance is invalid.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is invalid; otherwise, <c>false</c>.
        /// </value>
        public bool IsInvalid => this == Invalid;

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => this != Invalid;

        /// <summary>
        /// Gets a value indicating whether this instance is equal to the master file group id.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is master; otherwise, <c>false</c>.
        /// </value>
        public bool IsMaster => this == Master;

        /// <summary>
        /// Gets a value indicating whether this instance is equal to the primary file group id.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is primary; otherwise, <c>false</c>.
        /// </value>
        public bool IsPrimary => this == Primary;

        /// <summary>
        /// Gets a value indicating whether this instance is equal to a reserved file group id.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is reserved; otherwise, <c>false</c>.
        /// </value>
        public bool IsReserved => IsMaster || IsPrimary;
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"FileGroupId[{Value:X2}]";
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

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator <(FileGroupId left, FileGroupId right)
        {
            return (left.Value < right.Value);
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator >(FileGroupId left, FileGroupId right)
        {
            return (left.Value > right.Value);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(FileGroupId left, FileGroupId right)
        {
            return (left.Value == right.Value);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
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
                order = CompareTo((FileGroupId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public FileGroupId Clone()
        {
            return (FileGroupId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion
    }
}