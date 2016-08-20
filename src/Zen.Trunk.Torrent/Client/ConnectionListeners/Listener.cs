namespace Zen.Trunk.Torrent.Client
{
	using System;
	using System.Net;
	using Zen.Trunk.Torrent.Common;

	public abstract class Listener : IListener, IDisposable
	{
		#region Private Fields
		private IPEndPoint _localEndPoint;
		private ListenerStatus _status;
		#endregion

		#region Public Events
		/// <summary>
		/// Occurs when [status changed].
		/// </summary>
		public event EventHandler<EventArgs> StatusChanged;
		#endregion

		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Listener"/> class.
		/// </summary>
		/// <param name="localEndPoint">The local end point.</param>
		protected Listener(IPEndPoint localEndPoint)
		{
			_status = ListenerStatus.NotListening;
			_localEndPoint = localEndPoint;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the local end point.
		/// </summary>
		/// <value>The local end point.</value>
		public IPEndPoint LocalEndPoint
		{
			get
			{
				return _localEndPoint;
			}
		}

		/// <summary>
		/// Gets the status.
		/// </summary>
		/// <value>The status.</value>
		public ListenerStatus Status
		{
			get
			{
				return _status;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		public void Dispose()
		{
			DisposeManagedObjects();
		}

		public abstract void Start();

		public abstract void Stop();

		public void ChangeEndpoint(IPEndPoint endpoint)
		{
			_localEndPoint = endpoint;
			if (Status == ListenerStatus.Listening)
			{
				Stop();
				Start();
			}
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void DisposeManagedObjects()
		{
		}

		protected virtual void RaiseStatusChanged(ListenerStatus status)
		{
			if (_status != status)
			{
				_status = status;
				if (StatusChanged != null)
				{
					Toolbox.RaiseAsyncEvent<EventArgs>(StatusChanged, this, EventArgs.Empty);
				}
			}
		}
		#endregion
	}
}
