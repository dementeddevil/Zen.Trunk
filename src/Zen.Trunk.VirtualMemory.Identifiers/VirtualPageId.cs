using System;

namespace Zen.Trunk.VirtualMemory
{
	/// <summary>
	/// Simple value type which represents a Device Page Identifier.
	/// </summary>
	[Serializable]
	public struct VirtualPageId : IComparable, ICloneable
	{
        /// <summary>
        /// A virtual page id that represents the root page
        /// </summary>
        public static readonly VirtualPageId Zero = new VirtualPageId(0);

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualPageId"/> struct.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		public VirtualPageId(long virtualPageId)
		{
			DeviceId = new DeviceId((ushort)((virtualPageId >> 32) & 0xffff));
			PhysicalPageId = (uint)(virtualPageId & 0xffffffff);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualPageId"/> struct.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		public VirtualPageId(ulong virtualPageId)
		{
			DeviceId = new DeviceId((ushort)((virtualPageId >> 32) & 0xffff));
			PhysicalPageId = (uint)(virtualPageId & 0xffffffff);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualPageId"/> struct.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="physicalPageId">The physical page id.</param>
		public VirtualPageId(ushort deviceId, uint physicalPageId)
		{
			DeviceId = new DeviceId(deviceId);
			PhysicalPageId = physicalPageId;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualPageId"/> struct.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="physicalPageId">The physical page id.</param>
        public VirtualPageId(DeviceId deviceId, uint physicalPageId)
        {
            DeviceId = deviceId;
            PhysicalPageId = physicalPageId;
        }
        #endregion

        #region Public Properties
	    /// <summary>
		/// Gets the maximum allowable value for the physical page ID.
		/// </summary>
		public static uint MaximumPhysicalPageId => uint.MaxValue;

        /// <summary>
        /// Gets/sets the device ID.
        /// </summary>
		public DeviceId DeviceId { get; }

	    /// <summary>
		/// Gets/sets the physical page ID.
		/// </summary>
		public uint PhysicalPageId { get; }

	    /// <summary>
		/// Gets/sets the virtual page ID.
		/// </summary>
		public ulong Value => (((ulong)DeviceId.Value) << 32) | PhysicalPageId;

	    /// <summary>
		/// Gets a <see cref="T:VirtualPageId"/> representing the previous page.
		/// </summary>
		/// <value>A <see cref="T:VirtualPageId"/> object.</value>
		public VirtualPageId PreviousPage
		{
			get
			{
				if (PhysicalPageId == 0)
				{
					throw new InvalidOperationException("At first device page.");
				}

				return new VirtualPageId(DeviceId, PhysicalPageId - 1);
			}
		}

		/// <summary>
		/// Gets a <see cref="T:VirtualPageId"/> representing the next page.
		/// </summary>
		/// <value>A <see cref="T:VirtualPageId"/> object.</value>
		public VirtualPageId NextPage
		{
			get
			{
				if (PhysicalPageId == MaximumPhysicalPageId)
				{
					throw new InvalidOperationException("At last device page.");
				}

				return new VirtualPageId(DeviceId, PhysicalPageId + 1);
			}
		}
        #endregion

        #region Public Methods
        /// <summary>
        /// Gets a <see cref="VirtualPageId"/> that is offset from 
        /// the current object by the specified number of physical pages
        /// </summary>
        /// <param name="offsetIndex"></param>
        /// <returns></returns>
        public VirtualPageId Offset(int offsetIndex)
        {
            return new VirtualPageId(
                DeviceId, (uint)((int)PhysicalPageId + offsetIndex));
        }

        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
		{
			return $"VirtualPageId[DI:{DeviceId.Value:X4},PPI:{PhysicalPageId:X8}]";
		}

		/// <summary>
		/// Overridden. Tests obj for equality with this instance.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			var equal = false;
			if (obj is VirtualPageId)
			{
				var rhs = (VirtualPageId)obj;
				if (DeviceId == rhs.DeviceId &&
					PhysicalPageId == rhs.PhysicalPageId)
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
		public int CompareTo(VirtualPageId obj)
		{
			var order = DeviceId.CompareTo(obj.DeviceId);
			if (order == 0)
			{
				order = PhysicalPageId.CompareTo(obj.PhysicalPageId);
			}
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
        public static bool operator <(VirtualPageId left, VirtualPageId right)
		{
			if (left.DeviceId < right.DeviceId)
			{
				return true;
			}
			else if (left.DeviceId > right.DeviceId)
			{
				return false;
			}
			return left.PhysicalPageId < right.PhysicalPageId;
		}

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator >(VirtualPageId left, VirtualPageId right)
		{
			if (left.DeviceId > right.DeviceId)
			{
				return true;
			}
			if (left.DeviceId < right.DeviceId)
			{
				return false;
			}
			return left.PhysicalPageId > right.PhysicalPageId;
		}

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(VirtualPageId left, VirtualPageId right)
		{
            return left.DeviceId == right.DeviceId && left.PhysicalPageId == right.PhysicalPageId;
		}

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(VirtualPageId left, VirtualPageId right)
		{
            return left.DeviceId != right.DeviceId || left.PhysicalPageId != right.PhysicalPageId;
		}
		#endregion

		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			var order = -1;
			if (obj is VirtualPageId)
			{
				order = CompareTo((VirtualPageId)obj);
			}
			return order;
		}
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public VirtualPageId Clone()
		{
			return (VirtualPageId)MemberwiseClone();
		}
		object ICloneable.Clone()
		{
			return Clone();
		}
        #endregion
    }
}
