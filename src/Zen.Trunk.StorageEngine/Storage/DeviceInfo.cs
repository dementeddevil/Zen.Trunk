namespace Zen.Trunk.Storage
{
	using System;

	/// <summary>
	/// <b>DeviceInfo</b> contains base device definition data.
	/// </summary>
	/// <remarks>
	/// This object requires 162 bytes
	/// </remarks>
	public class DeviceInfo : BufferFieldWrapper
	{
		#region Private Fields
		private readonly BufferFieldUInt16 _id;
		private readonly BufferFieldStringFixed _name;
		private readonly BufferFieldStringFixed _pathName;
		#endregion

		#region Public Constructors
		public DeviceInfo()
		{
			_id = new BufferFieldUInt16();
			_name = new BufferFieldStringFixed(_id, 32);
			_pathName = new BufferFieldStringFixed(_name, 128);
		}

		public DeviceInfo(ushort id, string name, string pathName)
		{
			_id = new BufferFieldUInt16(id);
			_name = new BufferFieldStringFixed(_id, 32, name);
			_pathName = new BufferFieldStringFixed(_name, 128, pathName);
		}

		public DeviceInfo(DeviceInfo clone)
		{
			_id = new BufferFieldUInt16(clone.Id);
			_name = new BufferFieldStringFixed(_id, 32, clone.Name);
			_pathName = new BufferFieldStringFixed(_name, 128, clone.PathName);
		}
		#endregion

		#region Public Properties
		public ushort Id
		{
			get
			{
				return _id.Value;
			}
			set
			{
				_id.Value = value;
			}
		}

		public string Name
		{
			get
			{
				return _name.Value;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException(nameof(value));
				}
				if (value.Length > 32)
				{
					throw new ArgumentException("value too long - must be less than 32 characters.");
				}
				_name.Value = value;
			}
		}
	
		public string PathName
		{
			get
			{
				return _pathName.Value;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException(nameof(value));
				}
				if (value.Length > 128)
				{
					throw new ArgumentException("value too long - must be less than 128 characters.");
				}
				_pathName.Value = value;
			}
		}
		#endregion

		#region Protected Properties
		protected override BufferField FirstField => _id;

	    protected override BufferField LastField => _pathName;

	    #endregion

		#region Protected Methods
		#endregion
	}
}
