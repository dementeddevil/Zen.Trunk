
namespace Zen.Trunk.Storage.IO
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Runtime.ConstrainedExecution;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using Microsoft.Win32.SafeHandles;

	internal static class SafeNativeMethods
	{
		internal const int ERROR_INVALID_ADDRESS = 487;

		internal const int MEM_FREE = 0x10000;
		internal const int MEM_RELEASE = 0x8000;
		internal const int MEM_DECOMMIT = 0x4000;
		internal const int MEM_RESERVE = 0x2000;
		internal const int MEM_COMMIT = 0x1000;

		internal const int PAGE_NOACCESS = 0x01;
		internal const int PAGE_READONLY = 0x02;
		internal const int PAGE_READWRITE = 0x04;
		internal const int PAGE_WRITECOPY = 0x08;
		internal const int PAGE_EXECUTE = 0x10;
		internal const int PAGE_EXECUTE_READ = 0x20;
		internal const int PAGE_EXECUTE_READWRITE = 0x40;
		internal const int PAGE_EXECUTE_WRITECOPY = 0x80;

		internal const int PAGE_GUARD = 0x100;
		internal const int PAGE_NOCACHE = 0x200;
		internal const int PAGE_WRITECOMBINE = 0x400;

		internal static readonly IntPtr NULL = IntPtr.Zero;

		[StructLayout(LayoutKind.Sequential)]
		internal class SECURITY_ATTRIBUTES
		{
			internal int nLength;
			internal unsafe byte* pSecurityDescriptor = null;
			internal int bInheritHandle;
		}

		[StructLayout(LayoutKind.Explicit, Size = 8)]
		internal struct FILE_SEGMENT_ELEMENT
		{
			internal static readonly FILE_SEGMENT_ELEMENT Zero =
				new FILE_SEGMENT_ELEMENT(IntPtr.Zero);

			public FILE_SEGMENT_ELEMENT(IntPtr address)
			{
				Buffer = (ulong)address.ToInt64();
			}

			[FieldOffset(0)]
			public ulong Buffer;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SYSTEM_INFO
		{
			internal int dwOemId;
			internal int dwPageSize;
			internal IntPtr lpMinimumApplicationAddress;
			internal IntPtr lpMaximumApplicationAddress;
			internal IntPtr dwActiveProcessorMask;
			internal int dwNumberOfProcessors;
			internal int dwProcessorType;
			internal int dwAllocationGranularity;
			internal short wProcessorLevel;
			internal short wProcessorRevision;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool CancelIo(SafeFileHandle hFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern SafeFileHandle CreateFile(
			[MarshalAs(UnmanagedType.LPWStr)]string lpFileName,
			[MarshalAs(UnmanagedType.U4)]int dwDesiredAccess,
			[MarshalAs(UnmanagedType.U4)]FileShare dwShareMode,
			SECURITY_ATTRIBUTES securityAttrs,
			[MarshalAs(UnmanagedType.U4)]FileMode dwCreationDisposition,
			[MarshalAs(UnmanagedType.U4)]int dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool FlushFileBuffers(SafeFileHandle hFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern int FormatMessage(int dwFlags, IntPtr lpSource,
			int dwMessageId, int dwLanguageId, [Out]StringBuilder lpBuffer, int nSize,
			IntPtr arguments);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int GetFileSize(SafeFileHandle hFile,
			out int highSize);

		[DllImport("kernel32.dll")]
		internal static extern int GetFileType(SafeFileHandle handle);

		internal static string GetMessage(int errorCode)
		{
			var lpBuffer = new StringBuilder(0x200);
			if (FormatMessage(0x3200, NULL, errorCode, 0, lpBuffer,
				lpBuffer.Capacity, NULL) != 0)
			{
				return lpBuffer.ToString();
			}
			return string.Format(CultureInfo.InvariantCulture, "Unknown error {0}.", errorCode);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern void GetSystemInfo([MarshalAs(UnmanagedType.Struct)]ref SYSTEM_INFO lpSystemInfo);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetVolumeInformation(string drive,
			StringBuilder volumeName, int volumeNameBufLen,
			out int volSerialNumber, out int maxFileNameLen,
			out int fileSystemFlags, StringBuilder fileSystemName,
			int fileSystemNameBufLen);

		internal static int MakeHRFromErrorCode(int errorCode)
		{
			return (-2147024896 | errorCode);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
			Justification = "Object only disposed in the event of opening non-file object prior to throwing exception and normally returned to the caller.")]
		internal static SafeFileHandle SafeCreateFile(string lpFileName,
			int dwDesiredAccess, FileShare dwShareMode,
			SECURITY_ATTRIBUTES securityAttrs, FileMode dwCreationDisposition,
			int dwFlagsAndAttributes, IntPtr hTemplateFile)
		{
			var handle = CreateFile(lpFileName, dwDesiredAccess,
				dwShareMode, securityAttrs, dwCreationDisposition,
				dwFlagsAndAttributes, hTemplateFile);
			if (!handle.IsInvalid && (GetFileType(handle) != 1))
			{
				handle.Dispose();
				throw new NotSupportedException("Cannot create stream on non-files.");
			}
			return handle;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool LockFile(
			SafeFileHandle handle,
			uint offsetLow,
			uint offsetHigh,
			uint countLow,
			uint countHigh);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool UnlockFile(
			SafeFileHandle handle,
			uint offsetLow,
			uint offsetHigh,
			uint countLow,
			uint countHigh);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1415:DeclarePInvokesCorrectly",
			Justification = "This is the non-overlapped I/O definition where the overlapped structure must be null.")]
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool ReadFile(
			SafeFileHandle handle,
			byte* bytes,
			int numBytesToRead,
			out int numBytesRead,
			IntPtr mustBeZero);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool ReadFile(
			SafeFileHandle handle,
			byte* bytes,
			int numBytesToRead,
			IntPtr numBytesRead_mustBeZero,
			NativeOverlapped* overlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool ReadFileScatter(
			SafeFileHandle hFile,
			FILE_SEGMENT_ELEMENT[] segmentArray,
			uint bytesToRead,
			IntPtr reserved,
			NativeOverlapped* overlapped);

		internal static unsafe long SetFilePointer(SafeFileHandle handle,
			long offset, SeekOrigin origin, out int hr)
		{
			hr = 0;
			var lo = (int)offset;
			var hi = (int)(offset >> 0x20);
			lo = SetFilePointerWin32(handle, lo, &hi, (int)origin);
			if ((lo == -1) && ((hr = Marshal.GetLastWin32Error()) != 0))
			{
				return (long)(-1);
			}
			return (long)(((ulong)hi << 0x20) | ((uint)lo));
		}

		[DllImport("kernel32.dll", EntryPoint = "SetFilePointer", SetLastError = true)]
		private static extern unsafe int SetFilePointerWin32(
			SafeFileHandle handle, int lo, [Out]int* hi, int origin);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetEndOfFile(SafeFileHandle hFile);

		[DllImport("kernel32.dll")]
		internal static extern int SetErrorMode(int newMode);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1415:DeclarePInvokesCorrectly",
			Justification = "This signature is used for non-overlapped calls.")]
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool WriteFile(
			SafeFileHandle handle,
			byte* bytes,
			int numBytesToWrite,
			out int numBytesWritten,
			[In] IntPtr mustBeZero);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1415:DeclarePInvokesCorrectly",
			Justification = "This signature is used for overlapped calls.")]
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool WriteFile(
			SafeFileHandle handle,
			byte* bytes,
			int numBytesToWrite,
			IntPtr numBytesWritten_mustBeZero,
			NativeOverlapped* lpOverlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool WriteFileGather(
			SafeFileHandle hFile,
			FILE_SEGMENT_ELEMENT[] segmentArray,
			uint bytesToRead,
			IntPtr reserved,
			NativeOverlapped* overlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool DeviceIoControl(
			SafeFileHandle hFile,
			int controlCode,
			void* inBuffer,
			int inBufferSize,
			void* outBuffer,
			int outBufferSize,
			IntPtr bytesReturned_MustBeZero,
			NativeOverlapped* overlapped);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool DeviceIoControl(
			SafeFileHandle hFile,
			int controlCode,
			void* inBuffer,
			int inBufferSize,
			void* outBuffer,
			int outBufferSize,
			out int bytesReturned,
			IntPtr overlapped_MustBeZero);

		[DllImport("kernel32.dll", EntryPoint = "VirtualAlloc", SetLastError = true)]
		private static extern unsafe IntPtr DoVirtualAlloc(IntPtr address,
			UIntPtr numBytes, int commitOrReserve, int pageProtectionMode);

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[PrePrepareMethod]
		internal static unsafe SafeMemoryHandle VirtualReserve(
			UIntPtr numBytes, int pageProtectionMode)
		{
			var result = new SafeMemoryHandle();
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				var address = DoVirtualAlloc(IntPtr.Zero, numBytes, MEM_RESERVE, pageProtectionMode);
				if (address != IntPtr.Zero)
				{
					result.SetHandleInternal(address);
				}
			}

			if (result.IsInvalid)
			{
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
			return result;
		}

		internal static unsafe SafeCommitableMemoryHandle GetCommitableMemoryHandle(SafeMemoryHandle handle, int bufferSize)
		{
			var result = new SafeCommitableMemoryHandle();
			var success = false;
			handle.DangerousAddRef(ref success);
			if (!success)
			{
				throw new InvalidOperationException();
			}
			try
			{
				result.SetHandleInternal(handle.DangerousGetHandle(), new UIntPtr((ulong)bufferSize));
			}
			finally
			{
				handle.DangerousRelease();
			}
			return result;
		}

		internal static unsafe SafeCommitableMemoryHandle GetCommitableMemoryHandle(SafeCommitableMemoryHandle handle, long offset, int bufferSize)
		{
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			var result = new SafeCommitableMemoryHandle();
			var success = false;
			handle.DangerousAddRef(ref success);
			if (!success)
			{
				throw new InvalidOperationException();
			}
			try
			{
				// Dangerous pointer arithmetic (my favourite)
				var pointer = (byte*)handle.DangerousGetHandle().ToPointer();
				pointer = pointer + offset;
				result.SetHandleInternal(new IntPtr(pointer), new UIntPtr((ulong)bufferSize));
			}
			finally
			{
				handle.DangerousRelease();
			}
			return result;
		}

		internal static unsafe void VirtualCommit(SafeCommitableMemoryHandle existingAddress, int pageProtectionMode)
		{
			var success = false;
			existingAddress.DangerousAddRef(ref success);
			if (!success)
			{
				throw new InvalidOperationException();
			}
			try
			{
				var existingAddressPtr = existingAddress.DangerousGetHandle();
				var address = DoVirtualAlloc(existingAddressPtr, existingAddress.TotalBytes, MEM_COMMIT, pageProtectionMode);
				if (address == IntPtr.Zero)
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
				}
				if (address != existingAddressPtr)
				{
					// TODO: Throw up and log some shit
					throw new InvalidOperationException();
				}
			}
			finally
			{
				existingAddress.DangerousRelease();
			}
		}

		internal static unsafe void VirtualDecommit(SafeCommitableMemoryHandle existingAddress)
		{
			var success = false;
			existingAddress.DangerousAddRef(ref success);
			if (!success)
			{
				throw new InvalidOperationException();
			}
			try
			{
				var existingAddressPtr = existingAddress.DangerousGetHandle();
				if (!VirtualFree(existingAddressPtr, existingAddress.TotalBytes, MEM_DECOMMIT))
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
				}
			}
			finally
			{
				existingAddress.DangerousRelease();
			}
		}

#if PLATFORMx86
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "1",
			Justification = "This definition is only used for x86 builds.")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail),
		DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern unsafe bool VirtualProtectInternal(
			IntPtr address,
			int numBytes,
			int newPageProtectionMode,
			IntPtr oldPageProtectionMode);
#else
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "1",
			Justification = "This definition is only used for x64 builds.")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail),
		DllImport("kernel32.dll", EntryPoint = "VirtualProtect", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern unsafe bool VirtualProtectInternal(
			IntPtr address,
			long numBytes,
			int newPageProtectionMode,
			IntPtr oldPageProtectionMode);
#endif

		internal static unsafe int VirtualProtect(SafeCommitableMemoryHandle address, int newPageProtection)
		{
			var success = false;
			address.DangerousAddRef(ref success);
			if (!success)
			{
				throw new InvalidOperationException();
			}
			try
			{
				var addressPtr = address.DangerousGetHandle();
				var oldPageProtectionPtr = new IntPtr();
				if (!VirtualProtectInternal(addressPtr,
#if PLATFORMx86
 (int)address.TotalBytes,
#else
 address.TotalBytes,
#endif
 newPageProtection, oldPageProtectionPtr))
				{
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
				}
				return oldPageProtectionPtr.ToInt32();
			}
			finally
			{
				address.DangerousRelease();
			}
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern unsafe bool VirtualFree(IntPtr address, UIntPtr numBytes, int pageFreeMode);
	}

	public sealed class SafeMemoryHandle : SafeHandle
	{
		public SafeMemoryHandle()
			: base(IntPtr.Zero, true)
		{
		}

		public override bool IsInvalid
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[PrePrepareMethod]
			get
			{
				return (handle == IntPtr.Zero);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[PrePrepareMethod]
		protected override bool ReleaseHandle()
		{
			return SafeNativeMethods.VirtualFree(handle, UIntPtr.Zero, SafeNativeMethods.MEM_RELEASE);
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[PrePrepareMethod]
		internal void SetHandleInternal(IntPtr handle)
		{
			this.SetHandle(handle);
		}
	}

	[CLSCompliant(false)]
	public class SafeCommitableMemoryHandle : SafeHandle
	{
		private UIntPtr _totalBytes;

		public SafeCommitableMemoryHandle()
			: base(IntPtr.Zero, true)
		{
		}

		public UIntPtr TotalBytes => _totalBytes;

	    public override bool IsInvalid
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[PrePrepareMethod]
			get
			{
				return (handle == IntPtr.Zero);
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		[PrePrepareMethod]
		protected override bool ReleaseHandle()
		{
			return SafeNativeMethods.VirtualFree(handle, _totalBytes, SafeNativeMethods.MEM_DECOMMIT);
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[PrePrepareMethod]
		internal void SetHandleInternal(IntPtr handle, UIntPtr totalBytes)
		{
			this.SetHandle(handle);
			this._totalBytes = totalBytes;
		}
	}
}
