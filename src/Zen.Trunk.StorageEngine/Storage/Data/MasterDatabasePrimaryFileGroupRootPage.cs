// -----------------------------------------------------------------------
// <copyright file="MasterDatabasePrimaryFileGroupRootPage.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MasterDatabasePrimaryFileGroupRootPage : PrimaryFileGroupRootPage
	{
		#region Internal Objects
		internal class DatabaseRefInfo : BufferFieldWrapper
		{
			private BufferFieldByte _databaseId;
			private BufferFieldStringFixed _name;
			private BufferFieldStringFixed _primaryName;
			private BufferFieldStringFixed _primaryFilePathName;
			private BufferFieldBitVector8 _flags;

			public DatabaseRefInfo()
			{
				_databaseId = new BufferFieldByte();
				_name = new BufferFieldStringFixed(_databaseId, 32);
				_primaryName = new BufferFieldStringFixed(_name, 32);
				_primaryFilePathName = new BufferFieldStringFixed(_primaryName, 128);
				_flags = new BufferFieldBitVector8(_primaryFilePathName);
			}

			protected override BufferField FirstField
			{
				get
				{
					return _databaseId;
				}
			}

			protected override BufferField LastField
			{
				get
				{
					return _flags;
				}
			}

			public byte DatabaseId
			{
				get
				{
					return _databaseId.Value;
				}
				set
				{
					_databaseId.Value = value;
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

			public string PrimaryName
			{
				get
				{
					return _primaryName.Value;
				}
				set
				{
					_primaryName.Value = value;
				}
			}

			public string PrimaryFilePathName
			{
				get
				{
					return _primaryFilePathName.Value;
				}
				set
				{
					_primaryFilePathName.Value = value;
				}
			}

			public bool IsOnline
			{
				get
				{
					return _flags.GetBit(0);
				}
				set
				{
					_flags.SetBit(0, value);
				}
			}
		}
		#endregion

		#region Private Fields
		internal const ulong DBMasterSignature = 0x2948f3d3a123e502;
		internal const uint DBMasterSchemaVersion = 0x01000001;
		private BufferFieldInt32 _databaseCount;

		private Dictionary<byte, DatabaseRefInfo> _databases = new Dictionary<byte, DatabaseRefInfo>();
		#endregion

		#region Public Constructors
		public MasterDatabasePrimaryFileGroupRootPage()
		{
			_databaseCount = new BufferFieldInt32(base.LastHeaderField);
		}
		#endregion

		#region Public Properties
		public override uint MinHeaderSize
		{
			get
			{
				return base.MinHeaderSize + 4;
			}
		}
		#endregion

		#region Protected Properties
		protected override ulong RootPageSignature
		{
			get
			{
				return DBMasterSignature;
			}
		}

		protected override uint RootPageSchemaVersion
		{
			get
			{
				return DBMasterSchemaVersion;
			}
		}

		/// <summary>
		/// Overridden. Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField
		{
			get
			{
				return _databaseCount;
			}
		}
		#endregion

		#region Public Methods
		#endregion

		#region Internal Methods
		internal IEnumerable<DatabaseRefInfo> GetDatabaseEnumerator()
		{
			return _databases.Values;
		}

		internal byte AddDatabase(string databaseName, string primaryName, string primaryFilename)
		{
			for (byte deviceId = 1; ; ++deviceId)
			{
				if (!_databases.ContainsKey(deviceId))
				{
					_databases.Add(
						deviceId,
						new DatabaseRefInfo
						{
							DatabaseId = deviceId,
							Name = databaseName,
							PrimaryName = primaryName,
							PrimaryFilePathName = primaryFilename,
							IsOnline = true
						});
					SetDirty();
					return deviceId;
				}

				if (deviceId == 255)
				{
					throw new ArgumentException("Maximum number of databases reached.");
				}
			}
		}

		internal void RemoveDatabase(byte databaseId)
		{
			if (databaseId == 0)
			{
				throw new ArgumentException("Cannot remove master database");
			}
			if (_databases.ContainsKey(databaseId))
			{
				_databases.Remove(databaseId);
				SetDirty();
			}
		}

		/// <summary>
		/// Sets the database online.
		/// </summary>
		/// <param name="databaseId">The database id.</param>
		/// <param name="isOnline">if set to <c>true</c> [is online].</param>
		internal void SetDatabaseOnline(byte databaseId, bool isOnline)
		{
			if (databaseId == 0)
			{
				throw new ArgumentException("Cannot change online state of master database.");
			}
			if (_databases.ContainsKey(databaseId) &&
				_databases[databaseId].IsOnline != isOnline)
			{
				_databases[databaseId].IsOnline = isOnline;
				SetDirty();
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Reads the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void ReadData(BufferReaderWriter streamManager)
		{
			base.ReadData(streamManager);
			for (int index = 0; index < _databaseCount.Value; ++index)
			{
				DatabaseRefInfo info = new DatabaseRefInfo();
				info.Read(streamManager);
				_databases.Add(info.DatabaseId, info);
			}
		}

		/// <summary>
		/// Writes the page header block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteHeader(BufferReaderWriter streamManager)
		{
			_databaseCount.Value = _databases.Count;
			base.WriteHeader(streamManager);
		}

		/// <summary>
		/// Overridden. Writes the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteData(BufferReaderWriter streamManager)
		{
			base.WriteData(streamManager);
			foreach (DatabaseRefInfo info in _databases.Values)
			{
				info.Write(streamManager);
			}
		}

		protected override void OnInit(EventArgs e)
		{
			base.OnInit(e);
		}
		#endregion
	}
}
