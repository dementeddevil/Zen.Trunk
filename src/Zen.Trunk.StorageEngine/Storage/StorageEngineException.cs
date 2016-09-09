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
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageEngineException"/> class.
        /// </summary>
        public StorageEngineException ()
			: this ("Storage engine exception occurred")
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageEngineException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public StorageEngineException (string message)
			: base (message, "Zen.Trunk.Storage")
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageEngineException"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
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
