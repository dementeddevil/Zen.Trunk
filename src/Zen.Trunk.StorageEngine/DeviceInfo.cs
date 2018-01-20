using System;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.VirtualMemory;

namespace Zen.Trunk.Storage
{
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
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
        /// </summary>
        public DeviceInfo()
		{
			_id = new BufferFieldUInt16();
			_name = new BufferFieldStringFixed(_id, 32);
			_pathName = new BufferFieldStringFixed(_name, 128);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="pathName">Name of the path.</param>
        public DeviceInfo(DeviceId id, string name, string pathName)
		{
			_id = new BufferFieldUInt16(id.Value);
			_name = new BufferFieldStringFixed(_id, 32, name);
			_pathName = new BufferFieldStringFixed(_name, 128, pathName);
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
        /// </summary>
        /// <param name="clone">The clone.</param>
        public DeviceInfo(DeviceInfo clone)
		{
			_id = new BufferFieldUInt16(clone.Id.Value);
			_name = new BufferFieldStringFixed(_id, 32, clone.Name);
			_pathName = new BufferFieldStringFixed(_name, 128, clone.PathName);
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public DeviceId Id
		{
			get => new DeviceId(_id.Value);
            set => _id.Value = value.Value;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">value too long - must be less than 32 characters.</exception>
        public string Name
		{
			get => _name.Value;
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

        /// <summary>
        /// Gets or sets the name of the path.
        /// </summary>
        /// <value>
        /// The name of the path.
        /// </value>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">value too long - must be less than 128 characters.</exception>
        public string PathName
		{
			get => _pathName.Value;
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
        /// <summary>
        /// Gets the first buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField FirstField => _id;

        /// <summary>
        /// Gets the last buffer field object.
        /// </summary>
        /// <value>
        /// A <see cref="T:BufferField" /> object.
        /// </value>
        protected override BufferField LastField => _pathName;
	    #endregion
	}
}
