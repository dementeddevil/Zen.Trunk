using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using NAudio.Wave;
using Zen.Trunk.Storage.Locking;

namespace Zen.Trunk.Storage.Data.Audio
{
    /// <summary>
    /// DatabaseAudio is a high-level class that provides helpers for interacting with audio streams.
    /// </summary>
    /// <remarks>
    /// Currently the only WAV format files are supported and to ease the amount of work the transcoder
    /// needs to do, it is recommended that uncompressed, high bit-rate streams are used when streaming
    /// data into the database.
    /// </remarks>
    public class DatabaseAudio
    {
        private readonly ILifetimeScope _lifetimeScope;
        private AudioSchemaPage _schemaPage;
        private bool _canUpdateSchema;

        public DatabaseAudio(ILifetimeScope parentLifetimeScope, ObjectId objectId, bool isNewAudio)
        {
            Owner = parentLifetimeScope.Resolve<FileGroupDevice>();
            _lifetimeScope = parentLifetimeScope.BeginLifetimeScope(
                builder =>
                {
                    builder.RegisterInstance(this);
                    //builder.RegisterType<TableIndexManager>()
                    //    .As<IndexManager>()
                    //    .As<TableIndexManager>()
                    //    .SingleInstance();
                });
            ObjectId = objectId;
            IsNewAudio = isNewAudio;

#if DEBUG
            LockTimeout = TimeSpan.FromSeconds(30);
#else
			LockTimeout = TimeSpan.FromSeconds(10);
#endif
        }

        #region Public Properties
        /// <summary>
        /// Gets the file group id.
        /// </summary>
        /// <value>The file group id.</value>
        public FileGroupId FileGroupId => Owner.FileGroupId;

        /// <summary>
        /// Gets the name of the file group.
        /// </summary>
        /// <value>The name of the file group.</value>
        public string FileGroupName => Owner.FileGroupName;

        /// <summary>
        /// Gets the table object ID.
        /// </summary>
        public ObjectId ObjectId { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is new table.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is new table; otherwise, <c>false</c>.
        /// </value>
        public bool IsNewAudio { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is loading.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsLoading { get; internal set; }

        /// <summary>
        /// Gets the schema first logical identifier.
        /// </summary>
        /// <value>
        /// The schema first logical identifier.
        /// </value>
        public LogicalPageId SchemaFirstLogicalPageId { get; internal set; }

        /// <summary>
        /// Gets the schema last logical identifier.
        /// </summary>
        /// <value>
        /// The schema last logical identifier.
        /// </value>
        public LogicalPageId SchemaLastLogicalPageId { get; internal set; }

        /// <summary>
        /// Gets the schema root page.
        /// </summary>
        /// <value>
        /// The schema root page.
        /// </value>
        public AudioSchemaPage SchemaRootPage => _schemaPage;

        /// <summary>
        /// Gets or sets the first logical page identifier for table data.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        /// <exception cref="LockException">
        /// Thrown if locking the table schema for modification fails.
        /// </exception>
        /// <exception cref="LockTimeoutException">
        /// Thrown if locking the table schema for modification fails due to timeout.
        /// </exception>
        public LogicalPageId DataFirstLogicalPageId
        {
            get => SchemaRootPage.DataFirstLogicalPageId;
            private set => SchemaRootPage.DataFirstLogicalPageId = value;
        }

        /// <summary>
        /// Gets or sets the last logical page identifier for table data.
        /// </summary>
        /// <value>
        /// The logical page identifier.
        /// </value>
        /// <exception cref="LockException">
        /// Thrown if locking the table schema for modification fails.
        /// </exception>
        /// <exception cref="LockTimeoutException">
        /// Thrown if locking the table schema for modification fails due to timeout.
        /// </exception>
        public LogicalPageId DataLastLogicalPageId
        {
            get => SchemaRootPage.DataLastLogicalPageId;
            set => SchemaRootPage.DataLastLogicalPageId = value;
        }

        /// <summary>
        /// Gets a value indicating whether this table instance has any data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [has data]; otherwise, <c>false</c>.
        /// </value>
        public bool HasData => DataFirstLogicalPageId.Value != 0;

        /// <summary>
        /// Gets or sets the lock timeout.
        /// </summary>
        /// <value>The lock timeout.</value>
        public TimeSpan LockTimeout { get; set; }

        /// <summary>
        /// Gets the owner filegroup.
        /// </summary>
        /// <value>The database.</value>
        public FileGroupDevice Owner { get; }
        #endregion

        #region Internal Properties
        internal IDatabaseLockManager LockingManager => Owner.LifetimeScope.Resolve<IDatabaseLockManager>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads the audio schema starting from the specified logical id
        /// </summary>
        /// <param name="firstLogicalPageId">The first logical id.</param>
        /// <returns></returns>
        public async Task LoadSchemaAsync(LogicalPageId firstLogicalPageId)
        {
            SchemaFirstLogicalPageId = firstLogicalPageId;
            IsLoading = true;
            try
            {
                // Keep loading schema pages (these are linked)
                var logicalId = firstLogicalPageId;

                // Prepare page object and load
                _schemaPage = await LoadSchemaPageAsync(true, logicalId).ConfigureAwait(false);

                // Setup schema last logical id
                SchemaLastLogicalPageId = _schemaPage.LogicalPageId;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task AppendAudioData(WaveFileReader waveReader)
        {
            // Step 1: Format setup/compatibility check
            if (SchemaRootPage == null)
            {
                await InitSchemaPageAsync(true).ConfigureAwait(false);
                SchemaRootPage.WaveFormat = waveReader.WaveFormat;
            }
            else if (SchemaRootPage.WaveFormat != waveReader.WaveFormat)
            {
                // TODO: Add support for transcoding into the same format as the current entity
                throw new InvalidOperationException("Cannot append wave data when source format is different to audio object.");
            }

            // Step 2: Seek or create page/offset to start writing
            AudioDataPage currentPage;
            uint pageOffset = 0;
            if (!HasData)
            {
                currentPage = await InitDataPageAsync().ConfigureAwait(false);
                SchemaRootPage.DataFirstLogicalPageId = currentPage.LogicalPageId;
                SchemaRootPage.DataLastLogicalPageId = currentPage.LogicalPageId;
            }
            else
            {
                currentPage = await LoadDataPageAsync(DataLastLogicalPageId).ConfigureAwait(false);
                pageOffset = (uint)(SchemaRootPage.TotalBytes % currentPage.DataSize);
                if (pageOffset == currentPage.DataSize - 1)
                {
                    currentPage = await InitDataPageAndLinkAsync(currentPage).ConfigureAwait(false);
                    pageOffset = 0;
                }
            }

            // Step 3: Stream blocks into pages, linking them as we go (and update the totalbytes as we copy stuff in)
            byte[] buffer = new byte[currentPage.DataSize];
            var needToCreatePage = false;
            while (true)
            {
                int bytesRead = await waveReader.ReadAsync(buffer, 0, (int)(currentPage.DataSize - pageOffset)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                // We've managed to read more data so handle creating a page to store it
                if (needToCreatePage)
                {
                    currentPage = await InitDataPageAndLinkAsync(currentPage).ConfigureAwait(false);
                }

                // Copy data from source stream into destination page
                using (var pageStream = currentPage.CreateDataStream(false))
                {
                    if (pageOffset > 0)
                    {
                        pageStream.Seek(pageOffset, SeekOrigin.Begin);
                        pageOffset = 0;
                    }

                    await pageStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }

                SchemaRootPage.TotalBytes += bytesRead;
                currentPage.SetDataDirty();
                needToCreatePage = true;
            }

            // Ensure last page written is saved along with root page
            currentPage.Save();
            SchemaRootPage.Save();
            IsNewAudio = false;
        }
        #endregion

        #region Private Methods
        private AudioDataPage CreateDataPage()
        {
            var dataPage = new AudioDataPage();
            dataPage.ObjectId = ObjectId;
            dataPage.FileGroupId = FileGroupId;
            return dataPage;
        }

        private async Task<AudioDataPage> InitDataPageAsync()
        {
            var dataPage = CreateDataPage();
            dataPage.ReadOnly = false;
            await Owner
                .InitDataPageAsync(new InitDataPageParameters(dataPage, true, true, true, true))
                .ConfigureAwait(true);
            return dataPage;
        }

        public async Task<AudioDataPage> InitDataPageAndLinkAsync(AudioDataPage prevDataPage)
        {
            var dataPage = await InitDataPageAsync().ConfigureAwait(false);
            if (prevDataPage != null)
            {
                dataPage.PrevLogicalPageId = prevDataPage.LogicalPageId;
                prevDataPage.NextLogicalPageId = dataPage.LogicalPageId;
                prevDataPage.Save();
            }
            return dataPage;
        }

        private async Task<AudioDataPage> LoadDataPageAsync(LogicalPageId logicalId)
        {
            var dataPage = CreateDataPage();
            dataPage.LogicalPageId = logicalId;

            // Setup page locking and then load page
            await dataPage.SetObjectLockAsync(ObjectLockType.Shared).ConfigureAwait(false);

            await Owner
                .LoadDataPageAsync(new LoadDataPageParameters(dataPage, false, true))
                .ConfigureAwait(false);
            return dataPage;
        }

        private AudioSchemaPage CreateSchemaPage(bool isFirstSchemaPage)
        {
            var schemaPage = isFirstSchemaPage ? new AudioSchemaPage() : throw new ArgumentException("Audio samples do not currently support more than a single schema page", nameof(isFirstSchemaPage));
            schemaPage.ObjectId = ObjectId;
            schemaPage.FileGroupId = FileGroupId;
            return schemaPage;
        }

        private async Task<AudioSchemaPage> InitSchemaPageAsync(bool isFirstSchemaPage)
        {
            var schemaPage = CreateSchemaPage(isFirstSchemaPage);
            schemaPage.ReadOnly = false;
            await Owner
                .InitDataPageAsync(new InitDataPageParameters(schemaPage, true, true, true, true))
                .ConfigureAwait(true);
            return schemaPage;
        }

        private async Task<AudioSchemaPage> LoadSchemaPageAsync(bool isFirstSchemaPage, LogicalPageId logicalId)
        {
            var schemaPage = CreateSchemaPage(isFirstSchemaPage);
            schemaPage.LogicalPageId = logicalId;

            // Setup page locking and then load page
            await schemaPage.SetObjectLockAsync(ObjectLockType.Shared).ConfigureAwait(false);
            await schemaPage.SetSchemaLockAsync(SchemaLockType.SchemaStability).ConfigureAwait(false);

            await Owner
                .LoadDataPageAsync(new LoadDataPageParameters(schemaPage, false, true))
                .ConfigureAwait(false);
            return schemaPage;
        }
        #endregion
    }
}
