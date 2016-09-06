using System;
using System.Runtime.Serialization;

namespace Zen.Trunk.Storage
{
	/// <summary>
	/// The class <b>StorageException</b> defines the base exception type thrown
	/// from the storage sub-system class library.
	/// </summary>
	[Serializable]
	public class StorageEngineException : CoreException
	{
		#region Public Constructors
		public StorageEngineException ()
			: this ("Storage engine exception occurred")
		{
		}
		public StorageEngineException (string message)
			: base (message, "Zen.Trunk.Storage")
		{
		}
		public StorageEngineException (string message, Exception innerException)
			: base(message, "Zen.Trunk.Storage", innerException)
		{
		}
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Creates a CoreException object from serialisation information.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected StorageEngineException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
        #endregion
    }
}
