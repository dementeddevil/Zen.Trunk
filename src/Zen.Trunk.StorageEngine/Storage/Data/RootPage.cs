namespace Zen.Trunk.Storage.Data
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Threading.Tasks.Dataflow;
	using Locking;
	using IO;
	//using Zen.Trunk.Storage.Database.Index;

	/// <summary>
	/// <b>RootPage</b> is the first page held on the primary device of a file
	/// group.
	/// </summary>
	/// <remarks>
	/// This class serves as a common root page class for both the primary
	/// file-group and secondary file-groups.
	/// </remarks>
	public abstract class RootPage : LogicalPage
	{
		#region Public Constants
		public const byte StatusIsExpandable = 1;
		public const byte StatusIsExpandablePercent = 2;
		#endregion

		#region Private Fields
		private RootLockType _rootLock;

		private readonly BufferFieldUInt64 _signature;
		private readonly BufferFieldUInt32 _schemaVersion;
		private readonly BufferFieldBitVector8 _status;
		private readonly BufferFieldUInt32 _allocatedPages;
		private readonly BufferFieldUInt32 _maximumPages;
		private readonly BufferFieldUInt32 _growthPages;
		private readonly BufferFieldDouble _growthPercent;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:RootPage"/>.
		/// </summary>
		/// <param name="owner">The owner.</param>
		public RootPage()
		{
			_signature = new BufferFieldUInt64(base.LastHeaderField, RootPageSignature);
			_schemaVersion = new BufferFieldUInt32(_signature, RootPageSchemaVersion);
			_status = new BufferFieldBitVector8(_schemaVersion);
			_allocatedPages = new BufferFieldUInt32(_status);
			_maximumPages = new BufferFieldUInt32(_allocatedPages);
			_growthPages = new BufferFieldUInt32(_maximumPages);
			_growthPercent = new BufferFieldDouble(_growthPages);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets or sets the type of the lock.
		/// </summary>
		/// <value>The type of the lock.</value>
		public RootLockType RootLock
		{
			get
			{
				return _rootLock;
			}
			set
			{
				if (_rootLock != value)
				{
					var oldLock = _rootLock;
					try
					{
						_rootLock = value;
						LockPage();
					}
					catch
					{
						_rootLock = oldLock;
						throw;
					}
				}
			}
		}

		/// <summary>
		/// Overridden. Gets/sets the page status.
		/// </summary>
		/// <value></value>
		public override PageType PageType => PageType.Root;

	    /// <summary>
		/// Overridden. Returns boolean true indicating this is a root page.
		/// </summary>
		public override bool IsRootPage => true;

	    public override uint MinHeaderSize => base.MinHeaderSize + 29;

	    public byte Status
		{
			get
			{
				return _status.Value;
			}
			set
			{
				_status.Value = value;
				SetHeaderDirty();
			}
		}

		public uint AllocatedPages
		{
			get
			{
				return _allocatedPages.Value;
			}
			set
			{
				_allocatedPages.Value = value;
				SetHeaderDirty();
			}
		}

		public uint MaximumPages
		{
			get
			{
				return _maximumPages.Value;
			}
			set
			{
				_maximumPages.Value = value;
				SetHeaderDirty();
			}
		}

		public uint GrowthPages
		{
			get
			{
				return _growthPages.Value;
			}
			set
			{
				_growthPages.Value = value;
				if (value > 0)
				{
					_growthPercent.Value = 0.0;
					IsExpandable = true;
					IsExpandableByPercent = false;
				}
				else if (_growthPercent.Value == 0.0)
				{
					IsExpandable = IsExpandableByPercent = false;
				}
				SetHeaderDirty();
			}
		}

		public double GrowthPercent
		{
			get
			{
				return _growthPercent.Value;
			}
			set
			{
				_growthPercent.Value = value;
				if (value > 0.0)
				{
					_growthPages.Value = 0;
					IsExpandable = true;
					IsExpandableByPercent = true;
				}
				else if (_growthPages.Value == 0)
				{
					IsExpandable = IsExpandableByPercent = false;
				}
				SetHeaderDirty();
			}
		}

		public bool IsExpandable
		{
			get
			{
				return _status.GetBit(StatusIsExpandable);
			}
			private set
			{
				_status.SetBit(StatusIsExpandable, value);
			}
		}

		public bool IsExpandableByPercent
		{
			get
			{
				return _status.GetBit(StatusIsExpandablePercent);
			}
			private set
			{
				_status.SetBit(StatusIsExpandablePercent, value);
			}
		}
		#endregion

		#region Internal Properties
		internal RootLock TrackedLock
		{
			get
			{
				if (TrunkTransactionContext.Current == null)
				{
					throw new InvalidOperationException("No current transaction.");
				}

				// If we have no transaction locks then we should be in dispose
				var txnLocks = TrunkTransactionContext.TransactionLocks;

			    // Return the lock-owner block for this object instance
				return txnLocks?.GetOrCreateRootLock(FileGroupId);
			}
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

		#region Public Methods
		#endregion

		#region Protected Methods
		/// <summary>
		/// Performs operations on this instance prior to being initialised.
		/// </summary>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// <remarks>
		/// Overrides to this method must set their desired lock prior to
		/// calling the base class.
		/// The base class method will enable the locking primitives and call
		/// LockPage.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// </remarks>
		protected override void OnPreInit(EventArgs e)
		{
			RootLock = RootLockType.Exclusive;
			base.OnPreInit(e);
		}

		/// <summary>
		/// Overridden. Called by the system prior to loading the page
		/// from persistent storage.
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>
		/// Overrides to this method must set their desired lock prior to
		/// calling the base class.
		/// The base class method will enable the locking primitives and call
		/// LockPage.
		/// This mechanism ensures that all lock states have been set prior to
		/// the first call to LockPage.
		/// </remarks>
		protected override void OnPreLoad(EventArgs e)
		{
			if (RootLock == RootLockType.None)
			{
				RootLock = RootLockType.Shared;
			}
			base.OnPreLoad(e);
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
		protected override void ReadHeader(BufferReaderWriter streamManager)
		{
			base.ReadHeader(streamManager);
			ValidatePageSignatureAndVersion();
		}

		/// <summary>
		/// Writes the page header block to the specified buffer writer.
		/// </summary>
		/// <param name="streamManager">The stream manager.</param>
		protected override void WriteHeader(BufferReaderWriter streamManager)
		{
			WritePageSignatureAndVersion();
			base.WriteHeader(streamManager);
		}

		/// <summary>
		/// Called to apply suitable locks to this page.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnLockPage(IDatabaseLockManager lm)
		{
			base.OnLockPage(lm);
			try
			{
				TrackedLock.Lock(RootLock, LockTimeout);
				//lm.LockRoot(FileGroupId, RootLock, LockTimeout);
			}
			catch
			{
				base.OnUnlockPage(lm);
				throw;
			}
		}

		/// <summary>
		/// Called to remove locks applied to this page in a prior call to
		/// <see cref="M:DatabasePage.OnLockPage"/>.
		/// </summary>
		/// <param name="lm">A reference to the <see cref="IDatabaseLockManager"/>.</param>
		protected override void OnUnlockPage(IDatabaseLockManager lm)
		{
			TrackedLock.Unlock();
			//lm.UnlockRoot(FileGroupId);
			base.OnUnlockPage(lm);
		}
		#endregion

		#region Private Methods
		#endregion
	}
}
