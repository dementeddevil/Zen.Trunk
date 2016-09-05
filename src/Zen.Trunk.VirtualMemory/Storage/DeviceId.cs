using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents a Device Identifier.
    /// </summary>
    [Serializable]
    public struct DeviceId : IComparable, ICloneable
    {
        #region Public Fields
        public static readonly DeviceId Zero = new DeviceId(0);
        public static readonly DeviceId Primary = new DeviceId(1);
        public static readonly DeviceId FirstSecondary = new DeviceId(2);
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceId"/> struct.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public DeviceId(ushort deviceId)
        {
            Value = deviceId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the device ID.
        /// </summary>
        [CLSCompliant(false)]
        public ushort Value { get; }

        /// <summary>
        /// Gets the next device id.
        /// </summary>
        public DeviceId Next
        {
            get
            {
                if (Value == ushort.MaxValue)
                {
                    throw new InvalidOperationException("At last device.");
                }

                return new DeviceId((ushort)(Value + 1));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"DeviceId[{Value:X4}]";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is DeviceId)
            {
                var rhs = (DeviceId)obj;
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
        public int CompareTo(DeviceId obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(DeviceId left, DeviceId right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(DeviceId left, DeviceId right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(DeviceId left, DeviceId right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(DeviceId left, DeviceId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is DeviceId)
            {
                order = CompareTo((DeviceId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public DeviceId Clone()
        {
            return (DeviceId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion
    }
}