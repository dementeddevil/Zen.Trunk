using System;
using Microsoft.Win32.SafeHandles;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
	/// SafeMsiHandle provides a managed wrapper on a MSI handle object.
	/// </summary>
	public class SafeMsiHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SafeMsiHandle"/> class.
		/// </summary>
		/// <param name="preexistingHandle">The preexisting handle.</param>
		/// <param name="ownsHandle">if set to <c>true</c> [owns handle].</param>
		public SafeMsiHandle(IntPtr preexistingHandle, bool ownsHandle)
			: base(ownsHandle)
		{
			base.SetHandle(preexistingHandle);
		}

		private SafeMsiHandle()
			: base(true)
		{
		}

		/// <summary>
		/// Overridden. Executes the code required to free the handle.
		/// </summary>
		/// <returns>
		/// true if the handle is released successfully; otherwise, in the event of a catastrophic failure, false. In this case, it generates a ReleaseHandleFailed Managed Debugging Assistant.
		/// </returns>
		protected override bool ReleaseHandle()
		{
			var result = Win32Native.MsiCloseHandle(base.handle);
			if (result == 0)
			{
				return true;
			}
			return false;
		}
	}
}
