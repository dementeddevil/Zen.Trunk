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
			private readonly BufferFieldUInt16 _databaseId;
			private readonly BufferFieldStringFixed _name;
			private readonly BufferFieldStringFixed _primaryName;
			private readonly BufferFieldStringFixed _primaryFilePathName;
			private readonly BufferFieldBitVector8 _flags;

			public DatabaseRefInfo()
			{
				_databaseId = new BufferFieldUInt16();
				_name = new BufferFieldStringFixed(_databaseId, 32);
				_primaryName = new BufferFieldStringFixed(_name, 32);
				_primaryFilePathName = new BufferFieldStringFixed(_primaryName, 128);
				_flags = new BufferFieldBitVector8(_primaryFilePathName);
			}

			protected override BufferField FirstField => _databaseId;

		    protected override BufferField LastField => _flags;

		    public DatabaseId DatabaseId
			{
				get
				{
					return new DatabaseId(_databaseId.Value);
				}
				set
				{
					_databaseId.Value = value.Value;
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
		internal const ulong DbMasterSignature = 0x2948f3d3a123e502;
		internal const uint DbMasterSchemaVersion = 0x01000001;
		private readonly BufferFieldInt32 _databaseCount;

		private readonly Dictionary<DatabaseId, DatabaseRefInfo> _databases =
            new Dictionary<DatabaseId, DatabaseRefInfo>();
		#endregion

		#region Public Constructors
		public MasterDatabasePrimaryFileGroupRootPage()
		{
			_databaseCount = new BufferFieldInt32(base.LastHeaderField);
		}
		#endregion

		#region Public Properties
		public override uint MinHeaderSize => base.MinHeaderSize + 4;

	    #endregion

		#region Protected Properties
		protected override ulong RootPageSignature => DbMasterSignature;

	    protected override uint RootPageSchemaVersion => DbMasterSchemaVersion;

	    /// <summary>
		/// Overridden. Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _databaseCount;

	    #endregion

		#region Public Methods
		#endregion

		#region Internal Methods
		internal IEnumerable<DatabaseRefInfo> GetDatabaseEnumerator()
		{
			return _databases.Values;
		}

		internal DatabaseId AddDatabase(string databaseName, string primaryName, string primaryFilename)
		{
		    try
		    {
			    for (var deviceId = new DatabaseId(1); ; deviceId = deviceId.Next)
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
			    }
		    }
		    catch (ArgumentException)
		    {
		        throw new InvalidOperationException("Maximum number of databases has been reached.");
		    }
		}

		internal void RemoveDatabase(DatabaseId databaseId)
		{
			if (databaseId == DatabaseId.Zero)
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
		internal void SetDatabaseOnline(DatabaseId databaseId, bool isOnline)
		{
			if (databaseId == DatabaseId.Zero)
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
			for (var index = 0; index < _databaseCount.Value; ++index)
			{
				var info = new DatabaseRefInfo();
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
			foreach (var info in _databases.Values)
			{
				info.Write(streamManager);
			}
		}
		#endregion
	}
}
