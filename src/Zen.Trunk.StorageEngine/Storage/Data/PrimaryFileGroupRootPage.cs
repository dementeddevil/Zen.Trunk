namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Zen.Trunk.Storage.IO;

	/// <summary>
	/// The database root page object is responsible for tracking file-groups
	/// and other root-level database information.
	/// </summary>
	public class PrimaryFileGroupRootPage : RootPage
	{
		#region Internal Objects
		/*internal class IndexRefInfo : BufferFieldWrapper
	{
		private BufferFieldByte _fileGroupId;
		private BufferFieldByte _indexType;
		private RootIndexInfo _rootIndex;

		public IndexRefInfo ()
		{
			_fileGroupId = new BufferFieldByte ();
			_indexType = new BufferFieldByte (_fileGroupId);
		}

		protected override BufferField FirstField
		{
			get
			{
				return _fileGroupId;
			}
		}

		protected override BufferField LastField
		{
			get
			{
				return _indexType;
			}
		}

		internal byte FileGroupId
		{
			get
			{
				return _fileGroupId.Value;
			}
			set
			{
				_fileGroupId.Value = value;
			}
		}

		internal byte IndexType
		{
			get
			{
				return _indexType.Value;
			}
			set
			{
				_indexType.Value = value;
			}
		}

		internal RootIndexInfo RootIndex
		{
			get
			{
				return _rootIndex;
			}
			set
			{
				_rootIndex = value;
			}
		}

		protected override void DoRead (BufferReaderWriter streamManager)
		{
			base.DoRead (streamManager);
			if (IndexType == 1)
			{
				RootTableIndexInfo rtii = new RootTableIndexInfo ();
				rtii.Read (streamManager);
				_rootIndex = rtii;
			}
			else if (IndexType == 2)
			{
				RootSampleIndexInfo rsii = new RootSampleIndexInfo ();
				rsii.Read (streamManager);
				_rootIndex = rsii;
			}
		}

		protected override void DoWrite (BufferReaderWriter streamManager)
		{
			base.DoWrite (streamManager);
			_rootIndex.Write (streamManager);
		}
	}*/
		#endregion

		#region Private Fields
		internal const ulong DBSignature = 0x2948f3d3a123e500;
		internal const uint DBSchemaVersion = 0x01000001;

		private readonly BufferFieldInt32 _deviceCount;
		private readonly BufferFieldInt32 _indexCount;
		private readonly BufferFieldInt32 _objectCount;

		private readonly Dictionary<DeviceId, DeviceInfo> _devices = new Dictionary<DeviceId, DeviceInfo>();
		//private Dictionary<uint, IndexRefInfo> _indices = new Dictionary<uint,IndexRefInfo> ();
		private readonly Dictionary<ObjectId, ObjectRefInfo> _objects = new Dictionary<ObjectId, ObjectRefInfo>();
		#endregion

		#region Public Constructors
		public PrimaryFileGroupRootPage()
		{
			_deviceCount = new BufferFieldInt32(base.LastHeaderField);
			_indexCount = new BufferFieldInt32(_deviceCount);
			_objectCount = new BufferFieldInt32(_indexCount);
		}
		#endregion

		#region Public Properties
		public override uint MinHeaderSize => base.MinHeaderSize + 12;

	    public IEnumerable<DeviceInfo> Devices => from item in _devices.Values
	        select new DeviceInfo(item);

	    #endregion

		#region Protected Properties
		protected override ulong RootPageSignature => DBSignature;

	    protected override uint RootPageSchemaVersion => DBSchemaVersion;

	    /// <summary>
		/// Overridden. Gets the last header field.
		/// </summary>
		/// <value>The last header field.</value>
		protected override BufferField LastHeaderField => _objectCount;

	    #endregion

		#region Public Methods
		/// <summary>
		/// Adds the data devices to the file-group based on the information 
		/// contained in the page definition structures.
		/// </summary>
		/// <remarks>
		/// This method is responsible for adding all slave devices defined
		/// in the root page configuration to the page device.
		/// Called after the primary device has been opened.
		/// </remarks>
		/// <param name="pageDevice"></param>
		public Task CreateSlaveDataDevices(FileGroupDevice pageDevice)
		{
			var addTaskList = new List<Task>();
			foreach (var di in _devices.Values)
			{
				addTaskList.Add(pageDevice.AddDataDevice(
					new AddDataDeviceParameters(di.Name, di.PathName, di.Id)));
			}
			return TaskExtra.WhenAllOrEmpty(addTaskList.ToArray());
		}

		/// <summary>
		/// Adds the device info.
		/// </summary>
		/// <param name="info">The info.</param>
		public bool AddDeviceInfo(DeviceInfo info)
		{
			CheckReadOnly();

			// Add device
			// TODO: Check whether we can fit this device descriptor into our 
			//	page.
			_devices.Add(info.Id, info);
			++_deviceCount.Value;

			// Mark page as dirty
			SetDirty();
			return true;
		}

		public bool UpdateDeviceInfo(DeviceInfo info)
		{
			var result = false;
			CheckReadOnly();
			if (_devices.ContainsKey(info.Id))
			{
				// Add device
				// TODO: Check whether we can fit this device descriptor into our 
				//	page.
				_devices[info.Id] = info;

				// Mark page as dirty
				SetDirty();
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Removes the device.
		/// </summary>
		/// <param name="id">The id.</param>
		public void RemoveDevice(DeviceId id)
		{
			CheckReadOnly();
			if (_devices.ContainsKey(id))
			{
				// Remove device
				_devices.Remove(id);
				--_deviceCount.Value;
				SetDirty();
			}
		}

		public DeviceInfo GetDevice(DeviceId id)
		{
			// Return clone of the data held in the root page.
			return new DeviceInfo(_devices[id]);
		}

		public bool AddObjectInfo(ObjectRefInfo info)
		{
			CheckReadOnly();

			// TODO: Check we have enough space for this item on this page
			// Add to map
			_objects.Add(info.ObjectId, info);
			++_objectCount.Value;

			SetDirty();
			return true;
		}

		public bool UpdateObjectInfo(ObjectRefInfo info)
		{
			var result = false;
			CheckReadOnly();
			if (_objects.ContainsKey(info.ObjectId))
			{
				_objects[info.ObjectId] = info;
				SetDirty();
				result = true;
			}
			return result;
		}

		public ObjectRefInfo GetObject(string name)
		{
			return _objects.Values.FirstOrDefault((item) => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
			for (var index = 0; index < _deviceCount.Value; ++index)
			{
				var info = new DeviceInfo();
				info.Read(streamManager);
				_devices.Add(info.Id, info);
			}
			/*for (int index = 0; index < _indexCount.Value; ++index)
			{
				IndexRefInfo indexRef = new IndexRefInfo ();
				indexRef.Read (streamManager);
				indexRef.RootIndex.IndexFileGroup = _fileGroups[indexRef.FileGroupId].Name;

				_indices.Add (indexRef.RootIndex.ObjectId, indexRef);
			}*/
			for (var index = 0; index < _objectCount.Value; ++index)
			{
				var info = new ObjectRefInfo();
				info.Read(streamManager);
				_objects.Add(info.ObjectId, info);

				// Hookup file-group from page
				info.FileGroupId = FileGroupId;
				info.RootPageVirtualPageId = VirtualId;
			}
		}

		/// <summary>
		/// Overridden. Writes the data.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteData(BufferReaderWriter streamManager)
		{
			base.WriteData(streamManager);
			if (_deviceCount.Value != _devices.Count)
			{
				throw new InvalidOperationException("Device count mismatch.");
			}
			foreach (var di in _devices.Values)
			{
				di.Write(streamManager);
			}
			/*foreach (IndexRefInfo info in _indices.Values)
			{
				info.Write (streamManager);
			}*/
			foreach (var info in _objects.Values)
			{
				info.Write(streamManager);
			}
		}
		#endregion
	}
}