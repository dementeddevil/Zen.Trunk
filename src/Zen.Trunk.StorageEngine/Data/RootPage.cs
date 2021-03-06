using System;
using System.Threading.Tasks;
using Zen.Trunk.IO;
using Zen.Trunk.Storage.BufferFields;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data
{
    /// <summary>
    /// <b>RootPage</b> is the first page held on the primary device of a file
    /// group.
    /// </summary>
    /// <remarks>
    /// This class serves as a common root page class for both the primary
    /// file-group and secondary file-groups.
    /// </remarks>
    public abstract class RootPage : LogicalPage, IRootPage
    {
        #region Public Constants
        /// <summary>
        /// The status is expandable
        /// </summary>
        public const byte StatusIsExpandable = 1;

        /// <summary>
        /// The status is expandable percent
        /// </summary>
        public const byte StatusIsExpandablePercent = 2;
        #endregion

        #region Private Fields
        private readonly BufferFieldUInt64 _signature;
        private readonly BufferFieldUInt32 _schemaVersion;
        private readonly BufferFieldBitVector8 _status;
        private readonly BufferFieldUInt32 _allocatedPages;
        private readonly BufferFieldUInt32 _maximumPages;
        private readonly BufferFieldUInt32 _growthPages;
        private readonly BufferFieldDouble _growthPercent;
        #endregion

        #region Protected Constructors
        /// <summary>
        /// Initialises an instance of <see cref="T:RootPage"/>.
        /// </summary>
        protected RootPage()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _signature = new BufferFieldUInt64(base.LastHeaderField, RootPageSignature);
            // ReSharper disable once VirtualMemberCallInConstructor
            _schemaVersion = new BufferFieldUInt32(_signature, RootPageSchemaVersion);
            _status = new BufferFieldBitVector8(_schemaVersion);
            _allocatedPages = new BufferFieldUInt32(_status);
            _maximumPages = new BufferFieldUInt32(_allocatedPages);
            _growthPages = new BufferFieldUInt32(_maximumPages);
            _growthPercent = new BufferFieldDouble(_growthPages);

            PageType = PageType.Root;
        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the type of the lock.
        /// </summary>
        /// <value>The type of the lock.</value>
        public FileGroupRootLockType FileGroupLock { get; private set; }

        /// <summary>
        /// Overridden. Returns boolean true indicating this is a root page.
        /// </summary>
        public virtual bool IsRootPage => true;

        /// <summary>
        /// Gets the minimum number of bytes required for the header block.
        /// </summary>
        public override uint MinHeaderSize => base.MinHeaderSize + 29;

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public byte Status
        {
            get => _status.Value;
            set
            {
                _status.Value = value;
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets or sets the allocated pages.
        /// </summary>
        /// <value>
        /// The allocated pages.
        /// </value>
        public uint AllocatedPages
        {
            get => _allocatedPages.Value;
            set
            {
                _allocatedPages.Value = value;
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets or sets the maximum pages.
        /// </summary>
        /// <value>
        /// The maximum pages.
        /// </value>
        public uint MaximumPages
        {
            get => _maximumPages.Value;
            set
            {
                _maximumPages.Value = value;
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets or sets the growth pages.
        /// </summary>
        /// <value>
        /// The growth pages.
        /// </value>
        public uint GrowthPages
        {
            get => _growthPages.Value;
            set
            {
                _growthPages.Value = value;
                if (value > 0)
                {
                    _growthPercent.Value = 0.0;
                    IsExpandable = true;
                    IsExpandableByPercent = false;
                }
                else
                {
                    IsExpandable = IsExpandableByPercent = false;
                }
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets or sets the growth percent.
        /// </summary>
        /// <value>
        /// The growth percent.
        /// </value>
        public double GrowthPercent
        {
            get => _growthPercent.Value;
            set
            {
                _growthPercent.Value = value;
                if (value > 0.0)
                {
                    _growthPages.Value = 0;
                    IsExpandable = true;
                    IsExpandableByPercent = true;
                }
                else
                {
                    IsExpandable = IsExpandableByPercent = false;
                }
                SetHeaderDirty();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is expandable.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expandable; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpandable
        {
            get => _status.GetBit(StatusIsExpandable);
            private set => _status.SetBit(StatusIsExpandable, value);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is expandable by percent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is expandable by percent; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpandableByPercent
        {
            get => _status.GetBit(StatusIsExpandablePercent);
            private set => _status.SetBit(StatusIsExpandablePercent, value);
        }
        #endregion

        #region Protected Properties
        /// <summary>
        /// Gets the last header field.
        /// </summary>
        /// <value>The last header field.</value>
        protected override BufferField LastHeaderField => _growthPercent;

        /// <summary>
        /// Gets the root page signature.
        /// </summary>
        /// <value>The root page signature.</value>
        protected abstract ulong RootPageSignature
        {
            get;
        }

        /// <summary>
        /// Gets the root page schema version.
        /// </summary>
        /// <value>The root page schema version.</value>
        protected abstract uint RootPageSchemaVersion
        {
            get;
        }
        #endregion

        #region Private Properties
        private IFileGroupLock FileGroupRootLock =>
            TransactionLockOwnerBlock?.GetOrCreateFileGroupLock(FileGroupId);
        #endregion

        #region Public Methods
        /// <summary>
        /// Sets the root lock.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public async Task SetRootLockAsync(FileGroupRootLockType value)
        {
            if (FileGroupLock != value)
            {
                var oldLock = FileGroupLock;
                try
                {
                    FileGroupLock = value;
                    await LockPageAsync().ConfigureAwait(false);
                }
                catch
                {
                    FileGroupLock = oldLock;
                    throw;
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Performs operations on this instance prior to being initialised.
        /// </summary>
        /// <remarks>
        /// Overrides to this method must set their desired lock prior to
        /// calling the base class.
        /// The base class method will enable the locking primitives and call
        /// LockPage.
        /// This mechanism ensures that all lock states have been set prior to
        /// the first call to LockPage.
        /// </remarks>
        protected override async Task OnPreInitAsync()
        {
            await SetRootLockAsync(FileGroupRootLockType.Exclusive).ConfigureAwait(false);
            await base.OnPreInitAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Overridden. Called by the system prior to loading the page
        /// from persistent storage.
        /// </summary>
        /// <remarks>
        /// Overrides to this method must set their desired lock prior to
        /// calling the base class.
        /// The base class method will enable the locking primitives and call
        /// LockPage.
        /// This mechanism ensures that all lock states have been set prior to
        /// the first call to LockPage.
        /// </remarks>
        protected override async Task OnPreLoadAsync()
        {
            if (FileGroupLock == FileGroupRootLockType.None)
            {
                await SetRootLockAsync(FileGroupRootLockType.Shared).ConfigureAwait(false);
            }
            await base.OnPreLoadAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Validates the page signature and version.
        /// </summary>
        protected void ValidatePageSignatureAndVersion()
        {
            if (_signature.Value != RootPageSignature)
            {
            }
            if (_schemaVersion.Value != RootPageSchemaVersion)
            {
            }
        }

        /// <summary>
        /// Writes the page signature and version.
        /// </summary>
        protected void WritePageSignatureAndVersion()
        {
            _signature.Value = RootPageSignature;
            _schemaVersion.Value = RootPageSchemaVersion;
        }

        /// <summary>
        /// Reads the page header block from the specified buffer reader.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void ReadHeader(SwitchingBinaryReader streamManager)
        {
            base.ReadHeader(streamManager);
            ValidatePageSignatureAndVersion();
        }

        /// <summary>
        /// Writes the page header block to the specified buffer writer.
        /// </summary>
        /// <param name="streamManager">The stream manager.</param>
        protected override void WriteHeader(SwitchingBinaryWriter streamManager)
        {
            WritePageSignatureAndVersion();
            base.WriteHeader(streamManager);
        }

        /// <summary>
        /// Called to apply suitable locks to this page.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
        protected override async Task OnLockPageAsync(IDatabaseLockManager lockManager)
        {
            await base.OnLockPageAsync(lockManager).ConfigureAwait(false);
            try
            {
                await FileGroupRootLock.LockAsync(FileGroupLock, LockTimeout).ConfigureAwait(false);
            }
            catch
            {
                await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Called to remove locks applied to this page in a prior call to
        /// <see cref="M:DatabasePage.OnLockPage"/>.
        /// </summary>
        /// <param name="lockManager">A reference to the <see cref="IDatabaseLockManager"/>.</param>
        protected override async Task OnUnlockPageAsync(IDatabaseLockManager lockManager)
        {
            await FileGroupRootLock.UnlockAsync().ConfigureAwait(false);
            await base.OnUnlockPageAsync(lockManager).ConfigureAwait(false);
        }
        #endregion
    }
}
