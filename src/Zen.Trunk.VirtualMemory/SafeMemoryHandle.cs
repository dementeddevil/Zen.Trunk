using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Runtime.InteropServices.SafeHandle" />
    public sealed class SafeMemoryHandle : SafeHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeMemoryHandle"/> class.
        /// </summary>
        public SafeMemoryHandle()
            : base(IntPtr.Zero, true)
        {
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the handle value is invalid.
        /// </summary>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode" />
        /// </PermissionSet>
        public override bool IsInvalid
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [PrePrepareMethod]
            get
            {
                return (handle == IntPtr.Zero);
            }
        }

        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        /// <returns>
        /// true if the handle is released successfully; otherwise, in the event of a catastrophic failure, false. In this case, it generates a releaseHandleFailed MDA Managed Debugging Assistant.
        /// </returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [PrePrepareMethod]
        protected override bool ReleaseHandle()
        {
            return SafeNativeMethods.VirtualFree(handle, UIntPtr.Zero, SafeNativeMethods.MEM_RELEASE);
        }

        /// <summary>
        /// Sets the handle internal.
        /// </summary>
        /// <param name="handleObject">The handle object.</param>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [PrePrepareMethod]
        internal void SetHandleInternal(IntPtr handleObject)
        {
            SetHandle(handleObject);
        }
    }
}