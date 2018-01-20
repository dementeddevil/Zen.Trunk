namespace Zen.Trunk.Storage
{
	using System;
	using System.Runtime.Serialization;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.StorageEngineException" />
    [Serializable]
	public class PageException : StorageEngineException
	{
		#region Private Fields
		private readonly Page _page;
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        public PageException ()
			: this ((Page) null)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        public PageException (Page page)
			: this ("Generic page exception", page)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public PageException (string message)
			: this (message, (Page)null)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="page">The page.</param>
        public PageException (string message, Page page)
			: base (message)
		{
			_page = page;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public PageException (string message, Exception innerException)
			: this (message, null, innerException)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="page">The page.</param>
        /// <param name="innerException">The inner exception.</param>
        public PageException (string message, Page page, Exception innerException)
			: base (message, innerException)
		{
			_page = page;
		} 
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Creates a CoreException object from serialisation information.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected PageException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
			_page = (Page) info.GetValue ("Page", typeof (Page));
		}
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>
        /// The page.
        /// </value>
        public Page Page => _page;

        #endregion

        #region Public Methods
        /// <summary>
        /// Fills serialization information with details of this exception object.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException (nameof(info));
			}
			base.GetObjectData (info, context);
			info.AddValue ("Page", _page);
		}
		#endregion
	}

	/// <summary>
	/// <b>PageReadOnlyException</b> is thrown in an attempt to write
	/// to a read-only page.
	/// </summary>
	[Serializable]
	public class PageReadOnlyException : PageException
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="PageReadOnlyException"/> class.
        /// </summary>
        public PageReadOnlyException ()
			: this ("Page readonly exception occurred.")
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageReadOnlyException"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        public PageReadOnlyException (Page page) 
			: base ("Page readonly exception occurred.", page)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageReadOnlyException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public PageReadOnlyException (string message)
			: this (message, null)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="PageReadOnlyException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="page">The page.</param>
        public PageReadOnlyException (string message, Page page)
			: base (message, page)
		{
			
		}
	}
}
