namespace Zen.Trunk.Storage
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class PageException : StorageEngineException, ISerializable
	{
		#region Private Fields
		private readonly Page _page;
		#endregion

		#region Public Constructors
		public PageException ()
			: this ((Page) null)
		{
		}

		public PageException (Page page)
			: this ("Generic page exception", page)
		{
		}

		public PageException (string message)
			: this (message, (Page)null)
		{
		}

		public PageException (string message, Page page)
			: base (message)
		{
			_page = page;
		}

		public PageException (string message, Exception innerException)
			: this (message, null, innerException)
		{
		}

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
		public Page Page => _page;

	    #endregion

		#region Public Methods
		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException ("info");
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
		public PageReadOnlyException ()
			: this ("Page readonly exception occurred.")
		{
		}

		public PageReadOnlyException (Page page) 
			: base ("Page readonly exception occurred.", page)
		{
		}

		public PageReadOnlyException (string message)
			: this (message, null)
		{
		}

		public PageReadOnlyException (string message, Page page)
			: base (message, page)
		{
			
		}
	}
}
