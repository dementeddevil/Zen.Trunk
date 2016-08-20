namespace Zen.Trunk.Storage
{
	using System;
	using System.Globalization;

	/// <summary>
	/// Simple value type which represents a Device Page Identifier.
	/// </summary>
	[Serializable]
	public struct VirtualPageId : IComparable, ICloneable
	{
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
		[CLSCompliant(false)]
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
		[CLSCompliant(false)]
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
        [CLSCompliant(false)]
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
		[CLSCompliant(false)]
		public static uint MaximumPhysicalPageId => uint.MaxValue;

        /// <summary>
        /// Gets/sets the device ID.
        /// </summary>
        [CLSCompliant(false)]
		public DeviceId DeviceId { get; }

	    /// <summary>
		/// Gets/sets the physical page ID.
		/// </summary>
		[CLSCompliant(false)]
		public uint PhysicalPageId { get; }

	    /// <summary>
		/// Gets/sets the virtual page ID.
		/// </summary>
		[CLSCompliant(false)]
		public ulong Value => (((ulong)DeviceId.Value) << 32) | (ulong)PhysicalPageId;

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
		/// Overridden. Gets a string representation of the type.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"VirtualPageId{DeviceId.Value:X4}:{PhysicalPageId:X8}";
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

		public static bool operator >(VirtualPageId left, VirtualPageId right)
		{
			if (left.DeviceId > right.DeviceId)
			{
				return true;
			}
			else if (left.DeviceId < right.DeviceId)
			{
				return false;
			}
			return left.PhysicalPageId > right.PhysicalPageId;
		}

		public static bool operator ==(VirtualPageId left, VirtualPageId right)
		{
			var equal = false;
			if (left.DeviceId == right.DeviceId &&
				left.PhysicalPageId == right.PhysicalPageId)
			{
				equal = true;
			}
			return equal;
		}
		public static bool operator !=(VirtualPageId left, VirtualPageId right)
		{
			var notEqual = false;
			if (left.DeviceId != right.DeviceId ||
				left.PhysicalPageId != right.PhysicalPageId)
			{
				notEqual = true;
			}
			return notEqual;
		}
		#endregion

		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			var order = -1;
			if (obj is VirtualPageId)
			{
				order = ((VirtualPageId)this).CompareTo((VirtualPageId)obj);
			}
			return order;
		}
		#endregion

		#region ICloneable Members
		public VirtualPageId Clone()
		{
			return (VirtualPageId)MemberwiseClone();
		}
		object ICloneable.Clone()
		{
			return ((VirtualPageId)this).Clone();
		}
		#endregion
	}
}
