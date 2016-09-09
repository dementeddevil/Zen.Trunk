namespace Zen.Trunk.Storage.Log
{
    using System;

    /// <summary>
    /// Simple class type which represents a Log File Indentifier.
    /// </summary>
    [Serializable]
    public class LogFileId : IComparable, ICloneable
    {
        /// <summary>
        /// The zero
        /// </summary>
        public static readonly LogFileId Zero = new LogFileId(0);

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LogFileId"/> class.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        public LogFileId(uint fileId)
        {
            DeviceId = new DeviceId((ushort)((fileId >> 16) & 0xffff));
            Index = (ushort)(fileId & 0xffff);
            FileId = fileId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogFileId"/> class.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="index">The index.</param>
        public LogFileId(DeviceId deviceId, ushort index)
        {
            DeviceId = deviceId;
            Index = index;
            FileId = (((uint)DeviceId.Value) << 16) | (((uint)Index) & 0xffff);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the device ID.
        /// </summary>
        public DeviceId DeviceId { get; }

        /// <summary>
        /// Gets/sets the log file index position.
        /// </summary>
        public ushort Index { get; }

        /// <summary>
        /// Gets/sets the file ID.
        /// </summary>
        public uint FileId { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"LogFileId{{{DeviceId.Value:X}:{Index:X}}}";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is LogFileId)
            {
                var rhs = (LogFileId)obj;
                if (DeviceId == rhs.DeviceId &&
                    Index == rhs.Index)
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
            return FileId.GetHashCode();
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
        public virtual int CompareTo(LogFileId obj)
        {
            var order = DeviceId.CompareTo(obj.DeviceId);
            if (order == 0)
            {
                order = Index.CompareTo(obj.Index);
            }
            return order;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(LogFileId left, LogFileId right)
        {
            if (left == null && right == null)
            {
                return true;
            }
            if (left == null || right == null)
            {
                return false;
            }

            return left.DeviceId == right.DeviceId && left.Index == right.Index;
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(LogFileId left, LogFileId right)
        {
            if (left == null && right == null)
            {
                return false;
            }
            if (left == null || right == null)
            {
                return true;
            }

            return left.DeviceId != right.DeviceId || left.Index != right.Index;
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is LogFileId)
            {
                order = CompareTo((LogFileId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public LogFileId Clone()
        {
            return (LogFileId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion
    }
}
