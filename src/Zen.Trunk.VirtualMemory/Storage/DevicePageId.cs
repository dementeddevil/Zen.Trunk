namespace Zen.Trunk.Storage
{
	using System;
	using System.Globalization;

	/// <summary>
	/// Simple value type which represents a Device Page Identifier.
	/// </summary>
	[Serializable]
	public struct DevicePageId : IComparable, ICloneable
	{
		#region Private Fields
		/// <summary>
		/// The device identifier.
		/// </summary>
		private ushort _deviceId;

		/// <summary>
		/// The physical page identifier within the associated device.
		/// </summary>
		private uint _physicalPageId;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DevicePageId"/> struct.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		public DevicePageId(long virtualPageId)
		{
			_deviceId = (ushort)((virtualPageId >> 32) & 0xffff);
			_physicalPageId = (uint)(virtualPageId & 0xffffffff);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DevicePageId"/> struct.
		/// </summary>
		/// <param name="virtualPageId">The virtual page id.</param>
		[CLSCompliant(false)]
		public DevicePageId(ulong virtualPageId)
		{
			_deviceId = (ushort)((virtualPageId >> 32) & 0xffff);
			_physicalPageId = (uint)(virtualPageId & 0xffffffff);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DevicePageId"/> struct.
		/// </summary>
		/// <param name="deviceId">The device id.</param>
		/// <param name="physicalPageId">The physical page id.</param>
		[CLSCompliant(false)]
		public DevicePageId(ushort deviceId, uint physicalPageId)
		{
			_deviceId = deviceId;
			_physicalPageId = physicalPageId;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets/sets the device ID.
		/// </summary>
		[CLSCompliant(false)]
		public ushort DeviceId
		{
			get
			{
				return _deviceId;
			}
			set
			{
				_deviceId = value;
			}
		}

		/// <summary>
		/// Gets/sets the physical page ID.
		/// </summary>
		[CLSCompliant(false)]
		public uint PhysicalPageId
		{
			get
			{
				return _physicalPageId;
			}
			set
			{
				if (value > MaximumPhysicalPageId)
				{
					throw new ArgumentOutOfRangeException("value", value, "Physical page ID above maximum.");
				}
				_physicalPageId = value;
			}
		}

		/// <summary>
		/// Gets/sets the virtual page ID.
		/// </summary>
		[CLSCompliant(false)]
		public ulong VirtualPageId
		{
			get
			{
				return (((ulong)_deviceId) << 32) | (ulong)_physicalPageId;
			}
			set
			{
				_deviceId = (ushort)((value >> 32) & 0xffff);
				_physicalPageId = (uint)(value & 0xffffffff);
			}
		}

		/// <summary>
		/// Gets the maximum allowable value for the physical page ID.
		/// </summary>
		[CLSCompliant(false)]
		public static uint MaximumPhysicalPageId
		{
			get
			{
				return uint.MaxValue;
			}
		}

		/// <summary>
		/// Gets a <see cref="T:DevicePageId"/> representing the previous page.
		/// </summary>
		/// <value>A <see cref="T:DevicePageId"/> object.</value>
		public DevicePageId PreviousPage
		{
			get
			{
				if (_physicalPageId == 0)
				{
					throw new InvalidOperationException("At first device page.");
				}

				return new DevicePageId(_deviceId, _physicalPageId - 1);
			}
		}

		/// <summary>
		/// Gets a <see cref="T:DevicePageId"/> representing the next page.
		/// </summary>
		/// <value>A <see cref="T:DevicePageId"/> object.</value>
		public DevicePageId NextPage
		{
			get
			{
				if (_physicalPageId == MaximumPhysicalPageId)
				{
					throw new InvalidOperationException("At last device page.");
				}

				return new DevicePageId(_deviceId, _physicalPageId + 1);
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
			return string.Format(
				CultureInfo.InvariantCulture,
				"DevicePageId{{{0}:{1}}}",
				_deviceId.ToString("X4"),
				_physicalPageId.ToString("X8"));
		}

		/// <summary>
		/// Overridden. Tests obj for equality with this instance.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			bool equal = false;
			if (obj is DevicePageId)
			{
				DevicePageId rhs = (DevicePageId)obj;
				if (_deviceId == rhs._deviceId &&
					_physicalPageId == rhs._physicalPageId)
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
			return VirtualPageId.GetHashCode();
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
		public int CompareTo(DevicePageId obj)
		{
			int order = _deviceId.CompareTo(obj._deviceId);
			if (order == 0)
			{
				order = _physicalPageId.CompareTo(obj._physicalPageId);
			}
			return order;
		}

		public static bool operator <(DevicePageId left, DevicePageId right)
		{
			if (left._deviceId < right._deviceId)
			{
				return true;
			}
			else if (left._deviceId > right._deviceId)
			{
				return false;
			}
			return left._physicalPageId < right._physicalPageId;
		}

		public static bool operator >(DevicePageId left, DevicePageId right)
		{
			if (left._deviceId > right._deviceId)
			{
				return true;
			}
			else if (left._deviceId < right._deviceId)
			{
				return false;
			}
			return left._physicalPageId > right._physicalPageId;
		}

		public static bool operator ==(DevicePageId left, DevicePageId right)
		{
			bool equal = false;
			if (left._deviceId == right._deviceId &&
				left._physicalPageId == right._physicalPageId)
			{
				equal = true;
			}
			return equal;
		}
		public static bool operator !=(DevicePageId left, DevicePageId right)
		{
			bool notEqual = false;
			if (left._deviceId != right._deviceId ||
				left._physicalPageId != right._physicalPageId)
			{
				notEqual = true;
			}
			return notEqual;
		}
		#endregion

		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			int order = -1;
			if (obj is DevicePageId)
			{
				order = ((DevicePageId)this).CompareTo((DevicePageId)obj);
			}
			return order;
		}
		#endregion

		#region ICloneable Members
		public DevicePageId Clone()
		{
			return (DevicePageId)MemberwiseClone();
		}
		object ICloneable.Clone()
		{
			return ((DevicePageId)this).Clone();
		}
		#endregion
	}
}
