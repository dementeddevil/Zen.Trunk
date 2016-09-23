using System;
using System.Runtime.InteropServices;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class MsiView : IDisposable
	{
		#region Private Objects
		private enum ViewState
		{
			Created,
			Executed,
			Fetched,
			NoMoreData,
		}
		#endregion

		#region Private Fields
		private SafeMsiHandle _view;
		private ViewState _state;
		#endregion

		#region Internal Constructors
		internal MsiView(SafeMsiHandle view)
		{
			_view = view;
			_state = ViewState.Created;
		}
		#endregion

		#region Public Properties
		#endregion

		#region Public Methods
		/// <summary>
		/// Executes the specified record against this view instance.
		/// </summary>
		/// <param name="record">The record.</param>
		public void Execute(MsiRecord record)
		{
			if (_state == ViewState.Executed)
			{
				Close();
			}

			var handle = new HandleRef();
			if (record != null)
			{
				handle = new HandleRef(record, record.Handle.DangerousGetHandle());
			}
			Win32.MsiViewExecute(_view, handle);
			_state = ViewState.Executed;
		}

		/// <summary>
		/// Fetches the next row from this view instance.
		/// </summary>
		/// <returns></returns>
		public bool Fetch(MsiRecord record)
		{
			if (_state != ViewState.Executed &&
				_state != ViewState.Fetched)
			{
				throw new InvalidOperationException();
			}
			var result = record.FetchInternal(_view);
			if (!result)
			{
				_state = ViewState.NoMoreData;
			}
			else if (_state != ViewState.Fetched)
			{
				_state = ViewState.Fetched;
			}
			return result;
		}

		/// <summary>
		/// Modifies a row identified by this view using the information
		/// in the specified record.
		/// </summary>
		/// <param name="modifyFlags">The modify flags.</param>
		/// <param name="record">The record.</param>
		public void Modify(ViewModify modifyFlags, MsiRecord record)
		{
			if (_state != ViewState.Fetched)
			{
				throw new InvalidOperationException();
			}
			Win32.MsiViewModify(_view, modifyFlags, record.Handle);
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		public void Close()
		{
			if (_view != null &&
				!_view.IsClosed &&
				(_state == ViewState.Executed ||
				_state == ViewState.Fetched))
			{
				Win32.MsiViewClose(_view);
				_state = ViewState.Created;
			}
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		private void CheckViewHandle()
		{
			if (_view == null || _view.IsClosed || _view.IsInvalid)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="MsiView"/> is reclaimed by garbage collection.
		/// </summary>
		~MsiView()
		{
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, 
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources; 
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Close if we need to
				Close();

			    _view?.Dispose();
			}

			_view = null;
		}
		#endregion
	}
}
