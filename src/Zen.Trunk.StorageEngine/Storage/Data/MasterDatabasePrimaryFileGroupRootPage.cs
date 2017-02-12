// -----------------------------------------------------------------------
// <copyright file="MasterDatabasePrimaryFileGroupRootPage.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;

namespace Zen.Trunk.Storage.Data
{
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
			private readonly BufferFieldStringFixed _primaryDataPathName;
		    private readonly BufferFieldStringFixed _primaryLogPathName;
			private readonly BufferFieldBitVector8 _flags;

			public DatabaseRefInfo()
			{
				_databaseId = new BufferFieldUInt16();
				_name = new BufferFieldStringFixed(_databaseId, 32);
				_primaryName = new BufferFieldStringFixed(_name, 32);
				_primaryDataPathName = new BufferFieldStringFixed(_primaryName, 256);
                _primaryLogPathName = new BufferFieldStringFixed(_primaryDataPathName, 256);
				_flags = new BufferFieldBitVector8(_primaryDataPathName);
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

			public string PrimaryDataPathName
			{
				get { return _primaryDataPathName.Value; }
				set { _primaryDataPathName.Value = value; }
			}

		    public string PrimaryLogPathName
		    {
                get { return _primaryLogPathName.Value; }
                set { _primaryLogPathName.Value = value; }
		    }

			public bool IsOnline
            {
                get { return _flags.GetBit(0); }
				set { _flags.SetBit(0, value); }
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
        /// <summary>
        /// Initializes a new instance of the <see cref="MasterDatabasePrimaryFileGroupRootPage"/> class.
        /// </summary>
        public MasterDatabasePrimaryFileGroupRootPage()
		{
			_databaseCount = new BufferFieldInt32(base.LastHeaderField);
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum size of the header.
        /// </summary>
        /// <value>
        /// The minimum size of the header.
        /// </value>
        public override uint MinHeaderSize => base.MinHeaderSize + 4;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the root page signature.
        /// </summary>
        /// <value>
        /// The root page signature.
        /// </value>
        protected override ulong RootPageSignature => DbMasterSignature;

        /// <summary>
        /// Gets the root page schema version.
        /// </summary>
        /// <value>
        /// The root page schema version.
        /// </value>
        protected override uint RootPageSchemaVersion => DbMasterSchemaVersion;

	    /// <summary>
		/// Overridden. Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _databaseCount;
	    #endregion

		#region Internal Methods
		internal IEnumerable<DatabaseRefInfo> GetDatabaseEnumerator()
		{
			return _databases.Values;
		}

		internal DatabaseId AddDatabase(string databaseName, string primaryName, string primaryDataPathName, string primaryLogPathName)
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
							    PrimaryDataPathName = primaryDataPathName,
                                PrimaryLogPathName = primaryLogPathName,
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
		protected override void ReadData(SwitchingBinaryReader streamManager)
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
		protected override void WriteHeader(SwitchingBinaryWriter streamManager)
		{
			_databaseCount.Value = _databases.Count;
			base.WriteHeader(streamManager);
		}

		/// <summary>
		/// Overridden. Writes the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteData(SwitchingBinaryWriter streamManager)
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
