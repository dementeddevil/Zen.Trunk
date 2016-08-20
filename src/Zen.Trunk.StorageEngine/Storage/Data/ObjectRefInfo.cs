// -----------------------------------------------------------------------
// <copyright file="ObjectRefInfo.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class ObjectRefInfo : BufferFieldWrapper
	{
		private BufferFieldUInt32 _objectId;
		private BufferFieldByte _objectType;
		private BufferFieldStringFixed _name;
		private BufferFieldUInt64 _firstPageId;

		public ObjectRefInfo()
		{
			_objectId = new BufferFieldUInt32();
			_objectType = new BufferFieldByte(_objectId);
			_name = new BufferFieldStringFixed(_objectType, 32);
			_firstPageId = new BufferFieldUInt64(_name);
		}

		protected override BufferField FirstField
		{
			get
			{
				return _objectId;
			}
		}

		protected override BufferField LastField
		{
			get
			{
				return _firstPageId;
			}
		}

		/// <summary>
		/// Gets or sets the file group id.
		/// </summary>
		/// <value>The file group id.</value>
		/// <remarks>
		/// This value is not persisted.
		/// </remarks>
		public byte FileGroupId
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the root page virtual page id.
		/// </summary>
		/// <value>The root page virtual page id.</value>
		/// <remarks>
		/// This value is not persisted.
		/// </remarks>
		public ulong RootPageVirtualPageId
		{
			get;
			set;
		}

		public uint ObjectId
		{
			get
			{
				return _objectId.Value;
			}
			set
			{
				_objectId.Value = value;
			}
		}

		public ObjectType ObjectType
		{
			get
			{
				return (ObjectType)_objectType.Value;
			}
			set
			{
				_objectType.Value = (byte)value;
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
				_name.Value = value;
			}
		}

		public ulong FirstPageId
		{
			get
			{
				return _firstPageId.Value;
			}
			set
			{
				_firstPageId.Value = value;
			}
		}
	}

	public enum ObjectType : byte
	{
		Unknown = 0,
		Sample = 1,
		Table = 2,
		View = 3,
	}
}
