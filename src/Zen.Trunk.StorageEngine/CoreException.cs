using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Zen.Trunk
{
	/// <summary>
	/// The <b>CoreException</b> class is the base class for all exception
	/// objects used throughout the framework.
	/// </summary>
	[Serializable]
	public class CoreException : ApplicationException, ISerializable
	{
		#region Private Fields
		private readonly string _subSystem;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Creates a CoreException object with default values.
		/// </summary>
		public CoreException ()
		{
		}

		/// <summary>
		/// Creates a CoreException object with the given exception message.
		/// </summary>
		/// <param name="message"></param>
		public CoreException (string message)
			: base (message)
		{
		}

		/// <summary>
		/// Creates a CoreException object with the given message and
		/// inner exception object.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public CoreException (string message, Exception innerException)
			: base (message, innerException)
		{
		}

		/// <summary>
		/// Creates a CoreException object with the given message and
		/// raised from the given subsystem.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="subSystem"></param>
		public CoreException (string message, string subSystem)
			: base (message)
		{
			_subSystem = subSystem;
		}

		/// <summary>
		/// Creates a CoreException object with the given message, subsystem
		/// and inner exception object.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="subSystem"></param>
		/// <param name="innerException"></param>
		public CoreException (string message, string subSystem, Exception innerException)
			: base (message, innerException)
		{
			_subSystem = subSystem;
		}
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Creates a CoreException object from serialisation information.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected CoreException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
			_subSystem = (string) info.GetString ("SubSystem");
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets a string indicating the sub-system from which this exception
		/// was raised (or unknown if none was given at construction time.)
		/// </summary>
		public string SubSystem
		{
			get
			{
				if (string.IsNullOrEmpty (_subSystem))
				{
					return "<Unknown>";
				}
				return _subSystem;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Fills serialization information with details of this exception object.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		[SecurityPermission (SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException (nameof(info));
			}
			base.GetObjectData (info, context);
			info.AddValue ("SubSystem", _subSystem);
		}
		#endregion
	}
}
