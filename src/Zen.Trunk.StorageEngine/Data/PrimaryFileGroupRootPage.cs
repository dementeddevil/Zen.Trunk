using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Utils;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// The database root page object is responsible for tracking file-groups
    /// and other root-level database information.
    /// </summary>
    public class PrimaryFileGroupRootPage : RootPage, IPrimaryFileGroupRootPage
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
        internal const ulong DbSignature = 0x2948f3d3a123e500;
        internal const uint DbSchemaVersion = 0x01000001;

        private readonly BufferFieldInt32 _deviceCount;
        private readonly BufferFieldInt32 _objectCount;

        private readonly PageItemCollection<DeviceReferenceBufferFieldWrapper> _devices;
        private readonly PageItemCollection<ObjectReferenceBufferFieldWrapper> _objects;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryFileGroupRootPage"/> class.
        /// </summary>
        public PrimaryFileGroupRootPage()
        {
            _devices = new PageItemCollection<DeviceReferenceBufferFieldWrapper>(this);
            _objects = new PageItemCollection<ObjectReferenceBufferFieldWrapper>(this);

            _deviceCount = new BufferFieldInt32(base.LastHeaderField);
            _objectCount = new BufferFieldInt32(_deviceCount);
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the minimum size of the header.
        /// </summary>
        /// <value>
        /// The minimum size of the header.
        /// </value>
        public override uint MinHeaderSize => base.MinHeaderSize + 12;

        /// <summary>
        /// Gets the list of devices registered on this page.
        /// </summary>
        /// <value>
        /// The devices.
        /// </value>
        public ICollection<DeviceReferenceBufferFieldWrapper> Devices => _devices;

        /// <summary>
        /// Gets the list of objects registered on this page.
        /// </summary>
        /// <value>
        /// The objects.
        /// </value>
        public ICollection<ObjectReferenceBufferFieldWrapper> Objects => _objects;
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the root page signature.
        /// </summary>
        /// <value>
        /// The root page signature.
        /// </value>
        protected override ulong RootPageSignature => DbSignature;

        /// <summary>
        /// Gets the root page schema version.
        /// </summary>
        /// <value>
        /// The root page schema version.
        /// </value>
        protected override uint RootPageSchemaVersion => DbSchemaVersion;

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
            foreach (var di in _devices)
            {
                addTaskList.Add(pageDevice.AddDataDeviceAsync(
                    new AddDataDeviceParameters(di.Name, di.PathName, di.Id)));
            }
            return TaskExtra.WhenAllOrEmpty(addTaskList.ToArray());
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
            for (var index = 0; index < _deviceCount.Value; ++index)
            {
                var info = new DeviceReferenceBufferFieldWrapper();
                info.Read(streamManager);
                _devices.Add(info);
            }
            for (var index = 0; index < _objectCount.Value; ++index)
            {
                var info = new ObjectReferenceBufferFieldWrapper();
                info.Read(streamManager);
                _objects.Add(info);

                // Hookup file-group from page
                info.FileGroupId = FileGroupId;
                info.RootPageVirtualPageId = VirtualPageId;
            }
        }

        /// <summary>
        /// Overridden. Writes the data.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteData(SwitchingBinaryWriter streamManager)
        {
            base.WriteData(streamManager);
            if (_deviceCount.Value != _devices.Count)
            {
                throw new InvalidOperationException("Device count mismatch.");
            }
            foreach (var di in _devices)
            {
                di.Write(streamManager);
            }
            foreach (var info in _objects)
            {
                info.Write(streamManager);
            }
        }
        #endregion
    }
}