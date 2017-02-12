using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
namespace Zen.Trunk.VirtualMemory
{
    /// <summary>
    /// <c>AdvancedFileStream</c> replaces <see cref="T:FileStream"/>
    /// by exposing Win32 Scatter/Gather I/O capabilities and incorporating
    /// sparse file technology when file is backed onto an NTFS volume.
    /// </summary>
    public class AdvancedFileStream : Stream
    {
        #region Internal Objects
        internal static class Win32
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct FILE_ZERO_DATA_INFORMATION
            {
                internal long FileOffset;
                internal long BeyondFinalZero;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct FILE_ALLOCATED_RANGE_BUFFER
            {
                internal long FileOffset;
                internal long Length;
            }

            internal enum SparseControlCodes
            {
                FSCTL_SET_SPARSE = 0x000900C4,
                FSCTL_SET_ZERO_DATA = 0x000980C8,
                FSCTL_QUERY_ALLOCATED_RANGES = 0x000940CF,
            }

            internal static unsafe bool SetSparse(SafeFileHandle hFile,
                out int bytesReturned)
            {
                return SafeNativeMethods.DeviceIoControl(hFile,
                    (int)SparseControlCodes.FSCTL_SET_SPARSE,
                    null, 0, null, 0, out bytesReturned, IntPtr.Zero);
            }

            internal static unsafe bool SetSparse(SafeFileHandle hFile,
                NativeOverlapped* overlapped)
            {
                return SafeNativeMethods.DeviceIoControl(hFile,
                    (int)SparseControlCodes.FSCTL_SET_SPARSE,
                    null, 0, null, 0, IntPtr.Zero, overlapped);
            }

            internal static unsafe void SetZeroData(SafeFileHandle hFile,
                FILE_ZERO_DATA_INFORMATION zeroInfo, out int bytesReturned)
            {
                if (!SafeNativeMethods.DeviceIoControl(hFile,
                    (int)SparseControlCodes.FSCTL_SET_ZERO_DATA,
                    &zeroInfo, sizeof(FILE_ZERO_DATA_INFORMATION), null, 0,
                    out bytesReturned, IntPtr.Zero))
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    var errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Failed to set sparse zero region from {0} for {1} bytes.",
                        zeroInfo.FileOffset,
                        zeroInfo.BeyondFinalZero - zeroInfo.FileOffset);
                    throw new IOException(
                        errorMessage,
                        SafeNativeMethods.MakeHRFromErrorCode(errorCode));
                }
            }

            internal static unsafe bool SetZeroData(SafeFileHandle hFile,
                FILE_ZERO_DATA_INFORMATION zeroInfo, NativeOverlapped* overlapped)
            {
                return SafeNativeMethods.DeviceIoControl(hFile,
                    (int)SparseControlCodes.FSCTL_SET_ZERO_DATA,
                    &zeroInfo, sizeof(FILE_ZERO_DATA_INFORMATION), null, 0,
                    IntPtr.Zero, overlapped);
            }
        }
        #endregion

        #region Private Fields
        private static readonly IOCompletionCallback _ioCallback;
        private static int _pageSize;
        private static bool _gotPageSize;

        internal const int DefaultBufferSize = 0x1000;
        internal const int GENERIC_READ = -2147483648;
        private const int GENERIC_WRITE = 0x40000000;

        private const int ERROR_BROKEN_PIPE = 0x6d;
        private const int ERROR_HANDLE_EOF = 0x26;
        private const int ERROR_INVALID_PARAMETER = 0x57;
        private const int ERROR_IO_PENDING = 0x3e5;
        private const int ERROR_NO_DATA = 0xe8;

        private const int FILE_ATTRIBUTE_ENCRYPTED = 0x4000;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;
        private const int FILE_BEGIN = 0;
        private const int FILE_CURRENT = 1;
        private const int FILE_END = 2;
        private const int FILE_FLAG_WRITE_THROUGH = -2147483648;
        private const int FILE_FLAG_OVERLAPPED = 0x40000000;
        private const int FILE_FLAG_NO_BUFFERING = 0x20000000;
        private const int FILE_FLAG_RANDOM_ACCESS = 0x10000000;
        private const int FILE_FLAG_OPEN_NO_RECALL = 0x00100000;

        private string _fileName;
        private SafeFileHandle _handle;
        private FileInfo _fileInfo;
        private readonly object _syncObject = new object();

        private long _appendStart;
        private byte[] _buffer;
        private int _bufferSize;
        private bool _canRead;
        private bool _canSeek;
        private bool _canWrite;
        private bool _exposedHandle;
        private long _pos;
        private int _readLen;
        private int _readPos;
        private int _writePos;
        private bool _isAsync;
        private bool _isPipe;
        private bool _isSparse;
        private bool _scatterGatherEnabled;
        #endregion

        #region Public Constructors
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline",
            Justification = "Static can only be initialised inside an unsafe block.")]
        static AdvancedFileStream()
        {
            unsafe
            {
                _ioCallback = AsyncAFSCallback;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedFileStream"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        public AdvancedFileStream(string path, FileMode mode)
            : this(
                path,
                mode,
                mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
                FileShare.Read)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedFileStream"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        public AdvancedFileStream(string path, FileMode mode, FileAccess access)
            : this(path, mode, access, FileShare.Read)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedFileStream"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="enableScatterGather">if set to <c>true</c> [enable scatter gather].</param>
        public AdvancedFileStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize = DefaultBufferSize,
            FileOptions options = FileOptions.None,
            bool enableScatterGather = false)
        {
            var secAttrs = GetSecAttrs(share);
            Init(path, mode, access, 0, false, share, bufferSize, options,
                secAttrs, enableScatterGather);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedFileStream"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="enableScatterGather">if set to <c>true</c> [enable scatter gather].</param>
        /// <param name="fileSecurity">The file security.</param>
        public AdvancedFileStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            bool enableScatterGather,
            FileSecurity fileSecurity)
        {
            object pinningHandle;
            var secAttrs = GetSecAttrs(
                share, fileSecurity, out pinningHandle);
            try
            {
                Init(path, mode, access, 0, false, share, bufferSize,
                    enableScatterGather ? FileOptions.Asynchronous | options : options,
                    secAttrs, enableScatterGather);
            }
            finally
            {
                ((GCHandle?) pinningHandle)?.Free();
            }
        }
        #endregion

        #region Finalizers
        ~AdvancedFileStream()
        {
            if (_handle != null)
            {
                Dispose(false);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the sync root.
        /// </summary>
        /// <value>The sync root.</value>
        public object SyncRoot => _syncObject;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => _canRead;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => _canSeek;

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value></value>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => _canWrite;

        /// <summary>
        /// Gets a value indicating whether this instance is async.
        /// </summary>
        /// <value><c>true</c> if this instance is async; otherwise, <c>false</c>.</value>
        public virtual bool IsAsync => _isAsync;

        /// <summary>
        /// Gets a value indicating whether this instance is sparse.
        /// </summary>
        /// <value><c>true</c> if this instance is sparse; otherwise, <c>false</c>.</value>
        public bool IsSparse => _isSparse;

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <value></value>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Length
        {
            get
            {
                if (_handle.IsClosed)
                {
                    __Error.FileNotOpen();
                }
                if (!CanSeek)
                {
                    __Error.SeekNotSupported();
                }

                int highSize;
                var fileSize = SafeNativeMethods.GetFileSize(_handle, out highSize);
                if (fileSize == -1)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != 0)
                    {
                        __Error.WinIOError(errorCode, string.Empty);
                    }
                }

                // ReSharper disable once RedundantCast
                var totalLength = ((long)highSize << 0x20) | ((long)fileSize);
                if ((_writePos > 0) && ((_pos + _writePos) > totalLength))
                {
                    totalLength = _writePos + _pos;
                }
                return totalLength;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                new FileIOPermission(
                    FileIOPermissionAccess.PathDiscovery,
                    new[] { _fileName }).Demand();
                return _fileName;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <value></value>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Position
        {
            get
            {
                if (_handle.IsClosed)
                {
                    __Error.FileNotOpen();
                }
                if (!CanSeek)
                {
                    __Error.SeekNotSupported();
                }
                if (_exposedHandle)
                {
                    VerifyOSHandlePosition();
                }
                return (_pos + ((_readPos - _readLen) + _writePos));
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "value cannot be negative.");
                }
                if (_writePos > 0)
                {
                    FlushWrite(false);
                }
                _readPos = 0;
                _readLen = 0;
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Gets the safe file handle.
        /// </summary>
        /// <value>The safe file handle.</value>
        public virtual SafeFileHandle SafeFileHandle
        {
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode),
            SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                Flush();
                _readPos = 0;
                _readLen = 0;
                _writePos = 0;
                _exposedHandle = true;
                return _handle;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is enabled for 
        /// sparse file support.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance supports sparse files; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool IsSparseSupported => true;

        /// <summary>
        /// Gets the size of the system page.
        /// </summary>
        /// <value>The size of the page.</value>
        public static int SystemPageSize
        {
            get
            {
                if (!_gotPageSize)
                {
                    var systemInfo = new SafeNativeMethods.SYSTEM_INFO();
                    SafeNativeMethods.GetSystemInfo(ref systemInfo);
                    _pageSize = systemInfo.dwPageSize;
                    _gotPageSize = true;
                }
                return _pageSize;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Determines whether the pathname specified supports sparse files.
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns><c>true</c> if underlying volume supports sparse files
        /// and the file has sparse support (if it exists); otherwise, 
        /// <c>false</c>.</returns>
        /// <remarks>
        /// In all cases the volume indicated by the path will be checked for
        /// sparse file support - typically this means the volume must be an
        /// NTFS 5.0 or higher volume.
        /// If the volume test succeeds and the file exists on the specified
        /// path then the file itself will also be checked for sparse support.
        /// </remarks>
        public static bool IsSparseEnabledFile(string pathName)
        {
            // If file exists then check whether sparse has been enabled
            if (File.Exists(pathName))
            {
                // Check whether file attributes indicate sparse file.
                return ((File.GetAttributes(pathName) & FileAttributes.SparseFile) != 0);
            }
            return false;
        }

        /// <summary>
        /// Determines whether the volume associated with the specified path
        /// supports sparse files.
        /// </summary>
        /// <param name="pathName">Absolute path - either UNC or local file-system.</param>
        /// <returns><c>true</c> if underlying volume supports sparse files;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsSparseEnabledVolume(string pathName)
        {
            // Sanity check - must have absolute path information.
            if (!Path.IsPathRooted(pathName))
            {
                throw new ArgumentException("Must have absolute path.");
            }

            var root = Path.GetPathRoot(pathName);
            var volumeName = new StringBuilder(255);
            var fileSystemName = new StringBuilder(255);
            int volumeSerialNumber, maxFileNameLength, fileSystemFlags;
            SafeNativeMethods.GetVolumeInformation(root, volumeName, 255,
                out volumeSerialNumber, out maxFileNameLength,
                out fileSystemFlags, fileSystemName, 255);
            return ((fileSystemFlags & 0x40) != 0);
        }

        /// <summary>
        /// Sets the zero region block in a sparse file.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        public void SetZeroRegion(long offset, int count)
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (IsSparseSupported && _isSparse)
            {
                var zeroInfo =
                    new Win32.FILE_ZERO_DATA_INFORMATION();
                zeroInfo.FileOffset = offset;
                zeroInfo.BeyondFinalZero = offset + count;
                int bytesReturned;
                Win32.SetZeroData(_handle, zeroInfo, out bytesReturned);
            }
        }

        /// <summary>
        /// Cancels this any pending I/O operations issued by this thread.
        /// </summary>
        /// <remarks>
        /// This method will cancel all I/O operations issued by the current 
        /// system thread which may cause unexpected issues when executing in
        /// managed code.
        /// </remarks>
        public void Cancel()
        {
            if (!_isAsync)
            {
                throw new InvalidOperationException("Not allowed on synchronous stream.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            var result = SafeNativeMethods.CancelIo(_handle);
            if (!result)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new IOException(
                    "Failed to cancel pending asynchronous I/O.",
                    SafeNativeMethods.MakeHRFromErrorCode(errorCode));
            }
        }

        /// <summary>
        /// Begins an asynchronous read from the underlying file.
        /// </summary>
        /// <param name="buffer">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The num bytes.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns></returns>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public override IAsyncResult BeginRead(byte[] buffer, int offset,
            int count, AsyncCallback callback, object state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    "offset cannot be negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    "numBytes cannot be negative.");
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException(
                    "offset or count is invalid");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (_scatterGatherEnabled)
            {
                __Error.NotAllowedWhenSystemBufferDisabled();
            }
            if (!_isAsync)
            {
                return base.BeginRead(buffer, offset, count, callback, state);
            }
            if (!CanRead)
            {
                __Error.ReadNotSupported();
            }
            AdvancedStreamAsyncResult result;
            if (_isPipe)
            {
                if (_readPos >= _readLen)
                {
                    return BeginReadCore(buffer, offset, count, callback, state, 0);
                }
                var num = _readLen - _readPos;
                if (num > count)
                {
                    num = count;
                }
                Array.Copy(_buffer, _readPos, buffer, offset, num);
                _readPos += num;
                result = AdvancedStreamAsyncResult.CreateBufferedReadResult(
                    num, callback, state);
                result.CallUserCallback();
                return result;
            }
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            if (_readPos == _readLen)
            {
                if (count < _bufferSize)
                {
                    if (_buffer == null)
                    {
                        _buffer = new byte[_bufferSize];
                    }
                    IAsyncResult asyncResult = BeginReadCore(_buffer, 0, _bufferSize, null, null, 0);
                    _readLen = EndRead(asyncResult);
                    var num2 = _readLen;
                    if (num2 > count)
                    {
                        num2 = count;
                    }
                    Array.Copy(_buffer, 0, buffer, offset, num2);
                    _readPos = num2;
                    result = AdvancedStreamAsyncResult.CreateBufferedReadResult(num2, callback, state);
                    result.CallUserCallback();
                    return result;
                }
                _readPos = 0;
                _readLen = 0;
                return BeginReadCore(buffer, offset, count, callback, state, 0);
            }
            var num3 = _readLen - _readPos;
            if (num3 > count)
            {
                num3 = count;
            }
            Array.Copy(_buffer, _readPos, buffer, offset, num3);
            _readPos += num3;
            if ((num3 >= count))
            {
                result = AdvancedStreamAsyncResult.CreateBufferedReadResult(num3, callback, state);
                result.CallUserCallback();
                return result;
            }
            _readPos = 0;
            _readLen = 0;
            return BeginReadCore(buffer, offset + num3, count - num3, callback, state, num3);
        }

        /// <summary>
        /// Begins an asynchronous read that will fill the associated buffer
        /// collection in a single NTFS scattered read operation.
        /// </summary>
        /// <param name="buffers">A collection of <see cref="T:VirtualBuffer"/> objects.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="T:IAsyncResult"/> object.</returns>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public virtual IAsyncResult BeginReadScatter(
            IVirtualBuffer[] buffers, AsyncCallback callback, object state)
        {
            if (buffers == null || buffers.Length == 0)
            {
                throw new InvalidOperationException("Scatter/gather list empty.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (!CanRead)
            {
                __Error.ReadNotSupported();
            }
            if (!_scatterGatherEnabled)
            {
                __Error.ScatterGatherNotEnabled();
            }
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            return BeginReadFileScatterCore(buffers, callback, state);
        }

        /// <summary>
        /// Begins the write.
        /// </summary>
        /// <param name="buffer">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The num bytes.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns></returns>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public override IAsyncResult BeginWrite(byte[] buffer, int offset,
            int count, AsyncCallback callback, object state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative.");
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException("Invalid offset or count.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (_scatterGatherEnabled)
            {
                __Error.NotAllowedWhenSystemBufferDisabled();
            }
            if (!_isAsync)
            {
                return base.BeginWrite(buffer, offset, count, callback, state);
            }
            if (!CanWrite)
            {
                __Error.WriteNotSupported();
            }
            if (_isPipe)
            {
                if (_writePos > 0)
                {
                    FlushWrite(false);
                }
                return BeginWriteCore(buffer, offset, count, callback,
                    state);
            }
            if (_writePos == 0)
            {
                if (_readPos < _readLen)
                {
                    FlushRead();
                }
                _readPos = 0;
                _readLen = 0;
            }
            var unusedBufferSize = _bufferSize - _writePos;
            if (count <= unusedBufferSize)
            {
                if (_writePos == 0)
                {
                    _buffer = new byte[_bufferSize];
                }
                Array.Copy(buffer, offset, _buffer, _writePos, count);
                _writePos += count;
                var result = new AdvancedStreamAsyncResult();
                result._userCallback = callback;
                result._userStateObject = state;
                result._waitHandle = null;
                result._isWrite = true;
                result._numBufferedBytes = count;
                result.CallUserCallback();
                return result;
            }
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            return BeginWriteCore(buffer, offset, count, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous write that will write the associated buffer
        /// collection in a single NTFS gathered write operation.
        /// </summary>
        /// <param name="buffers">A collection of <see cref="T:VirtualBuffer"/> objects.</param>
        /// <param name="callback">The user callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns>An <see cref="T:IAsyncResult"/> object.</returns>
        [HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        public virtual IAsyncResult BeginWriteGather(
            IVirtualBuffer[] buffers, AsyncCallback callback, object state)
        {
            if (buffers == null || buffers.Length == 0)
            {
                throw new InvalidOperationException("Scatter/gather list empty.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (!CanWrite)
            {
                __Error.WriteNotSupported();
            }
            if (!_scatterGatherEnabled)
            {
                __Error.ScatterGatherNotEnabled();
            }
            if (_readPos > 0)
            {
                FlushRead();
            }
            return BeginWriteFileGatherCore(buffers, callback, state);
        }

        /// <summary>
        /// Waits for the pending asynchronous read to complete.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>
        /// The number of bytes read from the stream, between zero (0) and the number of bytes you requested. Streams return zero (0) only at the end of the stream, otherwise, they should block until at least one byte is available.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">asyncResult did not originate from a <see cref="M:System.IO.Stream.BeginRead(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see> method on the current stream. </exception>
        /// <exception cref="T:System.ArgumentNullException">asyncResult is null. </exception>
        public override unsafe int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            if (!_isAsync)
            {
                return base.EndRead(asyncResult);
            }
            var result = asyncResult as AdvancedStreamAsyncResult;
            if ((result == null) || result._isWrite || result._isScatterGather)
            {
                __Error.WrongAsyncResult();
            }
            // ReSharper disable once PossibleNullReferenceException
            if (1 == Interlocked.CompareExchange(ref result._EndXxxCalled, 1, 0))
            {
                __Error.EndReadCalledTwice();
            }
            WaitHandle handle = result._waitHandle;
            if (handle != null)
            {
                try
                {
                    handle.WaitOne();
                }
                finally
                {
                    handle.Close();
                }
            }
            var nativeOverlappedPtr = result._overlapped;
            if (nativeOverlappedPtr != null)
            {
                Overlapped.Free(nativeOverlappedPtr);
            }
            if (result._errorCode != 0)
            {
                __Error.WinIOError(result._errorCode, Path.GetFileName(_fileName));
            }
            return (result._numBytes + result._numBufferedBytes);
        }

        /// <summary>
        /// Ends an asynchronous read scatter operation.
        /// </summary>
        /// <returns>
        /// The number of bytes read from the stream, between zero (0) and
        /// the number of bytes you requested. Streams return zero (0) only 
        /// at the end of the stream, otherwise, they should block until at
        /// least one byte is available.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// asyncResult did not originate from a <see cref="M:BeginRead(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see>
        /// method on the current stream.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// asyncResult is null.
        /// </exception>
        public virtual unsafe int EndReadScatter(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            var result = asyncResult as AdvancedStreamAsyncResult;
            if ((result == null) || result._isWrite || !result._isScatterGather)
            {
                __Error.WrongAsyncResult();
            }
            // ReSharper disable once PossibleNullReferenceException
            if (1 == Interlocked.CompareExchange(ref result._EndXxxCalled, 1, 0))
            {
                __Error.EndReadCalledTwice();
            }
            WaitHandle handle = result._waitHandle;
            if (handle != null)
            {
                try
                {
                    handle.WaitOne();
                }
                finally
                {
                    handle.Close();
                }
            }
            var nativeOverlappedPtr = result._overlapped;
            if (nativeOverlappedPtr != null)
            {
                Overlapped.Free(nativeOverlappedPtr);
            }
            if (result._errorCode != 0)
            {
                __Error.WinIOError(result._errorCode, Path.GetFileName(_fileName));
            }
            return (result._numBytes + result._numBufferedBytes);
        }

        /// <summary>
        /// Ends an asynchronous write operation.
        /// </summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        /// <exception cref="T:System.ArgumentNullException">asyncResult is null. </exception>
        /// <exception cref="T:System.ArgumentException">asyncResult did not originate from a 
        /// <see cref="M:System.IO.Stream.BeginWrite(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see>
        /// method on the current stream. </exception>
        public override unsafe void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            if (!_isAsync)
            {
                base.EndWrite(asyncResult);
            }
            else
            {
                var result = asyncResult as AdvancedStreamAsyncResult;
                if ((result == null) || !result._isWrite || result._isScatterGather)
                {
                    __Error.WrongAsyncResult();
                }
                // ReSharper disable once PossibleNullReferenceException
                if (1 == Interlocked.CompareExchange(ref result._EndXxxCalled, 1, 0))
                {
                    __Error.EndWriteCalledTwice();
                }
                WaitHandle handle = result._waitHandle;
                if (handle != null)
                {
                    try
                    {
                        handle.WaitOne();
                    }
                    finally
                    {
                        handle.Close();
                    }
                }
                var nativeOverlappedPtr = result._overlapped;
                if (nativeOverlappedPtr != null)
                {
                    Overlapped.Free(nativeOverlappedPtr);
                }
                if (result._errorCode != 0)
                {
                    __Error.WinIOError(result._errorCode, Path.GetFileName(_fileName));
                }
            }
        }

        /// <summary>
        /// Ends an asynchronous write gather operation.
        /// </summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        /// <exception cref="T:System.ArgumentNullException">asyncResult is null. </exception>
        /// <exception cref="T:System.ArgumentException">asyncResult did not originate from a <see cref="M:System.IO.Stream.BeginWrite(System.Byte[],System.Int32,System.Int32,System.AsyncCallback,System.Object)"></see> method on the current stream. </exception>
        public virtual unsafe void EndWriteGather(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }
            var result = asyncResult as AdvancedStreamAsyncResult;
            if ((result == null) || !result._isWrite || !result._isScatterGather)
            {
                __Error.WrongAsyncResult();
            }
            // ReSharper disable once PossibleNullReferenceException
            if (1 == Interlocked.CompareExchange(ref result._EndXxxCalled, 1, 0))
            {
                __Error.EndWriteCalledTwice();
            }
            WaitHandle handle = result._waitHandle;
            if (handle != null)
            {
                try
                {
                    handle.WaitOne();
                }
                finally
                {
                    handle.Close();
                }
            }
            var nativeOverlappedPtr = result._overlapped;
            if (nativeOverlappedPtr != null)
            {
                Overlapped.Free(nativeOverlappedPtr);
            }
            if (result._errorCode != 0)
            {
                __Error.WinIOError(result._errorCode, Path.GetFileName(_fileName));
            }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this
        /// stream and causes any buffered data to be written to the
        /// underlying device.
        /// </summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        public override void Flush()
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (_writePos > 0)
            {
                Debug.Assert(!_scatterGatherEnabled);
                FlushWrite(false);
            }
            else if ((_readPos < _readLen) && CanSeek)
            {
                FlushRead();
            }
            if (_scatterGatherEnabled && CanWrite)
            {
                // While there is no buffering under this scenario
                //	the file metadata still requires flushing to disk
                SafeNativeMethods.FlushFileBuffers(_handle);
            }
            _readPos = 0;
            _readLen = 0;
        }

        /// <summary>
        /// Gets the <see cref="T:FileSecurity"/> object representing the
        /// NTFS access control settings.
        /// </summary>
        /// <returns>A <see cref="T:FileSecurity"/> object.</returns>
        public FileSecurity GetAccessControl()
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            return new FileSecurity(_fileName,
                AccessControlSections.Group |
                AccessControlSections.Owner |
                AccessControlSections.Access);
        }

        /// <summary>
        /// Locks the underlying file across the specified region for the 
        /// current process.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="length">The length.</param>
        public virtual void Lock(long position, long length)
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if ((position < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException((position < 0) ? "position" : "length", "position cannot be negative.");
            }

            var offsetLow = (uint)position;
            var offsetHigh = (uint)(position >> 0x20);
            var countLow = (uint)length;
            var countHigh = (uint)(length >> 0x20);
            if (!SafeNativeMethods.LockFile(_handle, offsetLow, offsetHigh, countLow, countHigh))
            {
                __Error.WinIOError();
            }
        }

        /// <summary>
        /// Reads the specified array.
        /// </summary>
        /// <param name="buffer">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), "null buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative.");
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException("invalid offset or length.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            var isSubBuffer = false;
            var bytesRead = _readLen - _readPos;
            if (bytesRead == 0)
            {
                if (!CanRead)
                {
                    __Error.ReadNotSupported();
                }
                if (_writePos > 0)
                {
                    FlushWrite(false);
                }
                if (!CanSeek || (count >= _bufferSize))
                {
                    bytesRead = ReadCore(buffer, offset, count);
                    _readPos = 0;
                    _readLen = 0;
                    return bytesRead;
                }
                if (_buffer == null)
                {
                    _buffer = new byte[_bufferSize];
                }
                bytesRead = ReadCore(_buffer, 0, _bufferSize);
                if (bytesRead == 0)
                {
                    return 0;
                }
                isSubBuffer = bytesRead < _bufferSize;
                _readPos = 0;
                _readLen = bytesRead;
            }
            if (bytesRead > count)
            {
                bytesRead = count;
            }
            Array.Copy(_buffer, _readPos, buffer, offset, bytesRead);
            _readPos += bytesRead;
            if ((!_isPipe && (bytesRead < count)) && !isSubBuffer)
            {
                var num2 = ReadCore(buffer, offset + bytesRead, count - bytesRead);
                bytesRead += num2;
                _readPos = 0;
                _readLen = 0;
            }
            return bytesRead;
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>
        /// The unsigned byte cast to an Int32, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override int ReadByte()
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if ((_readLen == 0) && !CanRead)
            {
                __Error.ReadNotSupported();
            }
            if (_readPos == _readLen)
            {
                if (_writePos > 0)
                {
                    FlushWrite(false);
                }
                if (_buffer == null)
                {
                    _buffer = new byte[_bufferSize];
                }
                _readLen = ReadCore(_buffer, 0, _bufferSize);
                _readPos = 0;
            }
            if (_readPos == _readLen)
            {
                return -1;
            }
            int num = _buffer[_readPos];
            _readPos++;
            return num;
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if ((origin < SeekOrigin.Begin) || (origin > SeekOrigin.End))
            {
                throw new ArgumentException("origin cannot be negative.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (!CanSeek)
            {
                __Error.SeekNotSupported();
            }
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (origin == SeekOrigin.Current)
            {
                offset -= _readLen - _readPos;
            }
            if (_exposedHandle)
            {
                VerifyOSHandlePosition();
            }
            var num = _pos + (_readPos - _readLen);
            var num2 = SeekCore(offset, origin);
            if ((_appendStart != -1) && (num2 < _appendStart))
            {
                SeekCore(num, SeekOrigin.Begin);
                throw new IOException("Attempt to seek prior to append start point.");
            }
            if (_readLen > 0)
            {
                if (num == num2)
                {
                    if (_readPos > 0)
                    {
                        Array.Copy(_buffer, _readPos, _buffer, 0, _readLen - _readPos);
                        _readLen -= _readPos;
                        _readPos = 0;
                    }
                    if (_readLen > 0)
                    {
                        SeekCore(_readLen, SeekOrigin.Current);
                    }
                    return num2;
                }
                if (((num - _readPos) < num2) && (num2 < ((num + _readLen) - _readPos)))
                {
                    var num3 = (int)(num2 - num);
                    Array.Copy(_buffer, _readPos + num3, _buffer, 0, _readLen - (_readPos + num3));
                    _readLen -= _readPos + num3;
                    _readPos = 0;
                    if (_readLen > 0)
                    {
                        SeekCore(_readLen, SeekOrigin.Current);
                    }
                    return num2;
                }
                _readPos = 0;
                _readLen = 0;
            }
            return num2;
        }

        /// <summary>
        /// Sets the access control.
        /// </summary>
        /// <param name="fileSecurity">The file security.</param>
        public void SetAccessControl(FileSecurity fileSecurity)
        {
            if (fileSecurity == null)
            {
                throw new ArgumentNullException(nameof(fileSecurity));
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            _fileInfo.SetAccessControl(fileSecurity);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        public override void SetLength(long value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "value cannot be negative.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (!CanSeek)
            {
                __Error.SeekNotSupported();
            }
            if (!CanWrite)
            {
                __Error.WriteNotSupported();
            }
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (_readPos < _readLen)
            {
                FlushRead();
            }
            _readPos = 0;
            _readLen = 0;
            if ((_appendStart != -1) && (value < _appendStart))
            {
                throw new IOException("cannot resize to smaller than initial file length when in append mode.");
            }
            SetLengthCore(value);
        }

        /// <summary>
        /// Unlocks the underlying file region previously locked by a prior
        /// call to <see cref="M:Lock"/>.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="length">The length.</param>
        /// <remarks>The specified region must match exactly with the region
        /// originally locked.
        /// </remarks>
        public virtual void Unlock(long position, long length)
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if ((position < 0) || (length < 0))
            {
                throw new ArgumentOutOfRangeException((position < 0) ? "position" : "length",
                    "position cannot be negative.");
            }

            var offsetLow = (uint)position;
            var offsetHigh = (uint)(position >> 0x20);
            var countLow = (uint)length;
            var countHigh = (uint)(length >> 0x20);
            if (!SafeNativeMethods.UnlockFile(_handle, offsetLow, offsetHigh, countLow, countHigh))
            {
                __Error.WinIOError();
            }
        }

        /// <summary>
        /// Writes the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">array;array is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// offset;offset cannot be negative.
        /// or
        /// count;count cannot be negative.
        /// </exception>
        /// <exception cref="System.ArgumentException">Invalid offset or count.</exception>
        public override void Write(byte[] array, int offset, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "array is null");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset cannot be negative.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative.");
            }
            if ((array.Length - offset) < count)
            {
                throw new ArgumentException("Invalid offset or count.");
            }
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (_writePos == 0)
            {
                if (!CanWrite)
                {
                    __Error.WriteNotSupported();
                }
                if (_readPos < _readLen)
                {
                    FlushRead();
                }
                _readPos = 0;
                _readLen = 0;
            }
            if (_writePos > 0)
            {
                var num = _bufferSize - _writePos;
                if (num > 0)
                {
                    if (num > count)
                    {
                        num = count;
                    }
                    Array.Copy(array, offset, _buffer, _writePos, num);
                    _writePos += num;
                    if (count == num)
                    {
                        return;
                    }
                    offset += num;
                    count -= num;
                }
                if (_isAsync)
                {
                    IAsyncResult asyncResult = BeginWriteCore(_buffer, 0, _writePos, null, null);
                    EndWrite(asyncResult);
                }
                else
                {
                    WriteCore(_buffer, 0, _writePos);
                }
            }
            if (count >= _bufferSize)
            {
                WriteCore(array, offset, count);
            }
            else if (count != 0)
            {
                if (_buffer == null)
                {
                    _buffer = new byte[_bufferSize];
                }
                Array.Copy(array, offset, _buffer, _writePos, count);
                _writePos = count;
            }
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            if (_handle.IsClosed)
            {
                __Error.FileNotOpen();
            }
            if (_writePos == 0)
            {
                if (!CanWrite)
                {
                    __Error.WriteNotSupported();
                }
                if (_readPos < _readLen)
                {
                    FlushRead();
                }
                _readPos = 0;
                _readLen = 0;
                if (_buffer == null)
                {
                    _buffer = new byte[_bufferSize];
                }
            }
            if (_writePos == _bufferSize)
            {
                FlushWrite(false);
            }
            _buffer[_writePos] = value;
            _writePos++;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:Stream" />
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged 
        /// resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (((_handle != null) && !_handle.IsClosed) && (_writePos > 0))
                {
                    FlushWrite(!disposing);
                }
            }
            finally
            {
                if ((_handle != null) && !_handle.IsClosed)
                {
                    _handle.Dispose();
                }
                _canRead = false;
                _canWrite = false;
                _canSeek = false;
                base.Dispose(disposing);
            }
        }
        #endregion

        #region Internal Properties
        internal string NameInternal
        {
            get
            {
                if (_fileName == null)
                {
                    return "<UnknownFileName>";
                }
                return _fileName;
            }
        }
        #endregion

        #region Private Methods
        private unsafe void Init(string path, FileMode mode, FileAccess access,
            int rights, bool useRights, FileShare share, int bufferSize,
            FileOptions options, SafeNativeMethods.SECURITY_ATTRIBUTES secAttrs,
            bool enableScatterGatherIO)
        {
            // Sanity check
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Cannot have null or empty path.");
            }
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new InvalidOperationException("AdvancedFileStream is only supported on NT platforms.");
            }
            if ((enableScatterGatherIO) && ((bufferSize % SystemPageSize) != 0))
            {
                throw new ArgumentException(
                    $"Buffer size must be multiple of system page size ({SystemPageSize}) bytes).", nameof(bufferSize));
            }

            // Ensure we have absolute path
            if (!Path.IsPathRooted(path))
            {
                // Non-rooted paths must be converted into absolute paths
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            }

            // Cache filename
            _fileName = path;
            _exposedHandle = false;
            _scatterGatherEnabled = enableScatterGatherIO;
            var fsRights = (FileSystemRights)rights;

            // Determine desired access rights
            int dwDesiredAccess;
            if (((!useRights && ((access & FileAccess.Write) == 0)) ||
                (useRights && ((fsRights & FileSystemRights.Write) == 0))) &&
                ((mode == FileMode.Truncate) || (mode == FileMode.CreateNew) ||
                (mode == FileMode.Create) || (mode == FileMode.Append)))
            {
                if (!useRights)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid file mode and access combination. {0} {1}.",
                        new object[] { mode, access }));
                }
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid file mode and rights combination. {0} {1}.",
                    new object[] { mode, fsRights }));
            }
            if (useRights && (mode == FileMode.Truncate))
            {
                if (fsRights != FileSystemRights.Write)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid file mode truncate and rights combination. {0} {1}.",
                        new object[] { mode, fsRights }));
                }
                useRights = false;
                access = FileAccess.Write;
            }
            if (!useRights)
            {
                dwDesiredAccess =
                    (access == FileAccess.Read) ? GENERIC_READ :
                    ((access == FileAccess.Write) ? GENERIC_WRITE :
                    GENERIC_READ | GENERIC_WRITE);
            }
            else
            {
                dwDesiredAccess = rights;
            }

            // Devices are not supported...
            if (_fileName.StartsWith(@"\\.\", StringComparison.Ordinal))
            {
                throw new ArgumentException("Devices are not supported.");
            }

            // Demand code-access permission to file
            var ioAccess = FileIOPermissionAccess.NoAccess;
            if ((!useRights && ((access & FileAccess.Read) != 0)) ||
                (useRights && ((fsRights & FileSystemRights.ReadAndExecute) != 0)))
            {
                if (mode == FileMode.Append)
                {
                    throw new ArgumentException(
                        "Cannot open file in append mode with only read access.");
                }
                ioAccess |= FileIOPermissionAccess.Read;
            }
            if ((!useRights && ((access & FileAccess.Write) != 0)) ||
                (useRights && ((fsRights & (FileSystemRights.TakeOwnership |
                FileSystemRights.ChangePermissions | FileSystemRights.Delete |
                FileSystemRights.Write | FileSystemRights.DeleteSubdirectoriesAndFiles)) != 0)))
            {
                if (mode == FileMode.Append)
                {
                    ioAccess |= FileIOPermissionAccess.Append;
                }
                else
                {
                    ioAccess |= FileIOPermissionAccess.Write;
                }
            }
            var control = ((secAttrs != null) && (secAttrs.pSecurityDescriptor != null))
                ? AccessControlActions.Change : AccessControlActions.None;
            new FileIOPermission(ioAccess, control, new[] { _fileName }).Demand();

            // Remove inherit from share mode
            share &= ~FileShare.Inheritable;

            // Append is really open/create followed by seek to end
            var isAppend = (mode == FileMode.Append);
            if (mode == FileMode.Append)
            {
                mode = FileMode.OpenOrCreate;
            }

            // Deal with asynchronous settings
            if ((options & FileOptions.Asynchronous) != FileOptions.None)
            {
                _isAsync = true;
            }
            else
            {
                // Remove async and scatter/gather support
                options &= ~FileOptions.Asynchronous;
                enableScatterGatherIO = false;
            }

            // Setup flags and attributes
            var dwFlagsAndAttributes = (uint)options;
            dwFlagsAndAttributes |= FILE_FLAG_OPEN_NO_RECALL;
            if (enableScatterGatherIO)
            {
                dwFlagsAndAttributes |= FILE_FLAG_NO_BUFFERING;
            }

            // Prepare to create the file
            var newMode = SafeNativeMethods.SetErrorMode(1);
            try
            {
                unchecked
                {
                    _handle = SafeNativeMethods.SafeCreateFile(path, dwDesiredAccess,
                        share, secAttrs, mode, (int)dwFlagsAndAttributes,
                        SafeNativeMethods.NULL);
                }
                if (_handle.IsInvalid)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == __Error.ERROR_PATH_NOT_FOUND)
                    {
                        string root = null;
                        if (_fileName != null)
                        {
                            root = Path.GetPathRoot(_fileName);
                        }
                        if (_fileName.Equals(root))
                        {
                            errorCode = __Error.ERROR_ACCESS_DENIED;
                        }
                    }
                    var canReportPath = false;
                    try
                    {
                        new FileIOPermission(
                            FileIOPermissionAccess.PathDiscovery,
                            new[] { _fileName }).Demand();
                        canReportPath = true;
                    }
                    catch (SecurityException)
                    {
                    }
                    if (canReportPath)
                    {
                        __Error.WinIOError(errorCode, _fileName);
                    }
                    else
                    {
                        __Error.WinIOError(errorCode, Path.GetFileName(_fileName));
                    }
                }
            }
            finally
            {
                SafeNativeMethods.SetErrorMode(newMode);
            }
            if (SafeNativeMethods.GetFileType(_handle) != 1)
            {
                _handle.Close();
                throw new NotSupportedException("Not supported on non-files.");
            }

            if (_isAsync)
            {
                var boundHandle = false;
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                try
                {
                    boundHandle = ThreadPool.BindHandle(_handle);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                    if (!boundHandle)
                    {
                        _handle.Close();
                    }
                }
                if (!boundHandle)
                {
                    throw new IOException("Failed to bind handle.");
                }
            }

            // Setup stream capabilities
            if (!useRights)
            {
                _canRead = (access & FileAccess.Read) != 0;
                _canWrite = (access & FileAccess.Write) != 0;
            }
            else
            {
                _canRead = (fsRights & FileSystemRights.ReadData) != 0;
                _canWrite = ((fsRights & FileSystemRights.WriteData) != 0) ||
                    ((fsRights & FileSystemRights.AppendData) != 0);
            }
            _canSeek = true;

            // Setup state machine
            _isPipe = false;
            _pos = 0;
            _bufferSize = bufferSize;
            _readPos = 0;
            _readLen = 0;
            _writePos = 0;
            if (isAppend)
            {
                _appendStart = SeekCore(0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }

            // Cache file information we might need later
            _fileInfo = new FileInfo(path);

            // If the existing file does not have sparse support then enable.
            if (IsSparseEnabledVolume(path) &&
                !IsSparseEnabledFile(path))
            {
                EnableSparse();
            }
        }

        /// <summary>
        /// Enables sparse file support for the current file-stream.
        /// </summary>
        private void EnableSparse()
        {
            // Sanity check
            if (SafeFileHandle.IsClosed || SafeFileHandle.IsInvalid)
            {
                throw new InvalidOperationException("File is invalid or closed.");
            }

            // Must be writable...
            if (CanWrite)
            {
                // Enable sparse files
                int bytesReturned;
                _isSparse = Win32.SetSparse(SafeFileHandle, out bytesReturned);

                // TODO: Derived classes must walk the file in a manner deemed
                //	appropriate and mark the zero-length regions.
            }
        }

        private static unsafe void AsyncAFSCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
        {
            var ar = (AdvancedStreamAsyncResult)Overlapped.Unpack(pOverlapped).AsyncResult;
            ar._numBytes = (int)numBytes;
            if ((errorCode == ERROR_BROKEN_PIPE) || (errorCode == ERROR_NO_DATA))
            {
                errorCode = 0;
            }
            ar._errorCode = (int)errorCode;
            ar._completedSynchronously = false;
            ar._isComplete = true;

            var waitHandle = ar._waitHandle;
            if ((waitHandle != null) && !waitHandle.Set())
            {
                __Error.WinIOError();
            }

            var callback = ar._userCallback;
            if (callback != null)
            {
                callback(ar);
            }
        }

        private unsafe AdvancedStreamAsyncResult BeginReadCore(byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, object stateObject, int numBufferedBytesRead)
        {
            NativeOverlapped* overlappedPtr;
            var ar = new AdvancedStreamAsyncResult();
            ar._handle = _handle;
            ar._userCallback = userCallback;
            ar._userStateObject = stateObject;
            ar._isWrite = false;
            ar._numBufferedBytes = numBufferedBytesRead;
            ar._waitHandle = new ManualResetEvent(false);
            var overlapped = new Overlapped(0, 0, IntPtr.Zero, ar);
            if (userCallback != null)
            {
                overlappedPtr = overlapped.Pack(_ioCallback, bytes);
            }
            else
            {
                overlappedPtr = overlapped.UnsafePack(null, bytes);
            }
            ar._overlapped = overlappedPtr;
            if (CanSeek)
            {
                var length = Length;
                if (_exposedHandle)
                {
                    VerifyOSHandlePosition();
                }
                if ((_pos + numBytes) > length)
                {
                    if (_pos <= length)
                    {
                        numBytes = (int)(length - _pos);
                    }
                    else
                    {
                        numBytes = 0;
                    }
                }
                overlappedPtr->OffsetLow = (int)_pos;
                overlappedPtr->OffsetHigh = (int)(_pos >> 0x20);
                SeekCore(numBytes, SeekOrigin.Current);
            }
            int hr;
            if ((ReadFileNative(_handle, bytes, offset, numBytes, overlappedPtr, out hr) == -1) && (numBytes != -1))
            {
                if (hr == ERROR_BROKEN_PIPE)
                {
                    overlappedPtr->InternalLow = IntPtr.Zero;
                    ar.CallUserCallback();
                    return ar;
                }
                if (hr == ERROR_IO_PENDING)
                {
                    return ar;
                }
                if (!_handle.IsClosed && CanSeek)
                {
                    SeekCore(0, SeekOrigin.Current);
                }
                if (hr == ERROR_HANDLE_EOF)
                {
                    __Error.EndOfFile();
                    return ar;
                }
                __Error.WinIOError(hr, string.Empty);
            }
            return ar;
        }

        private unsafe AdvancedStreamAsyncResult BeginReadFileScatterCore(
            IVirtualBuffer[] buffers, AsyncCallback userCallback, object stateObject)
        {
            // Prepare gather array
            var bufferCount = buffers.Length;
            var elemCount = buffers[0].BufferSize * bufferCount / VirtualBuffer.SystemPageSize;
            var elements =
                new SafeNativeMethods.FILE_SEGMENT_ELEMENT[elemCount + 1];
            for (var bufferIndex = 0; bufferIndex < bufferCount; ++bufferIndex)
            {
                var buffer = buffers[bufferIndex];

                // Add buffer to list in "SystemPageSize" sized chunks.
                var elemPerBuffer = buffer.BufferSize / VirtualBuffer.SystemPageSize;
                var elemOffset = elemPerBuffer * bufferIndex;
                for (var elemIndex = 0; elemIndex < elemPerBuffer; ++elemIndex)
                {
                    elements[elemOffset + elemIndex] = new SafeNativeMethods.FILE_SEGMENT_ELEMENT(
                        new IntPtr(((VirtualBuffer)buffer).Buffer + (VirtualBuffer.SystemPageSize * elemIndex)));
                }
            }

            // Calculate total number of pages and bytes to transfer
            var numBytes = elemCount * SystemPageSize;

            // Prepare async helper object
            var ar = new AdvancedStreamAsyncResult();
            ar._handle = _handle;
            ar._userCallback = userCallback;
            ar._userStateObject = stateObject;
            ar._isWrite = false;
            ar._isScatterGather = true;
            ar._numBufferedBytes = numBytes;
            var completeEvent = new ManualResetEvent(false);
            ar._waitHandle = completeEvent;

            // Ensure we don't get our buffers freed too early
            ar._pinnedBuffers = new GCHandle[buffers.Length];
            var index = 0;
            foreach (var buffer in buffers)
            {
                ar._pinnedBuffers[index++] = GCHandle.Alloc(buffer);
            }

            // Prepare overlapped structure
            NativeOverlapped* overlappedPtr;
            var overlapped = new Overlapped(0, 0, IntPtr.Zero, ar);
            if (userCallback != null)
            {
                overlappedPtr = overlapped.Pack(_ioCallback, elements);
            }
            else
            {
                overlappedPtr = overlapped.UnsafePack(null, elements);
            }
            ar._overlapped = overlappedPtr;

            int hr;
            if ((ReadFileScatterNative(elements, numBytes, overlappedPtr, out hr) == -1) && (numBytes != -1))
            {
                if (hr == ERROR_BROKEN_PIPE)
                {
                    overlappedPtr->InternalLow = IntPtr.Zero;
                    ar.CallUserCallback();
                    return ar;
                }
                if (hr == ERROR_IO_PENDING)
                {
                    return ar;
                }
                if (!_handle.IsClosed && CanSeek)
                {
                    SeekCore(0, SeekOrigin.Current);
                }
                if (hr == ERROR_HANDLE_EOF)
                {
                    __Error.EndOfFile();
                    return ar;
                }
                __Error.WinIOError(hr, string.Empty);
            }
            return ar;
        }

        private unsafe AdvancedStreamAsyncResult BeginWriteCore(byte[] buffer, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
        {
            NativeOverlapped* overlappedPtr;
            var ar = new AdvancedStreamAsyncResult();
            ar._handle = _handle;
            ar._userCallback = userCallback;
            ar._userStateObject = stateObject;
            ar._isWrite = true;
            ar._waitHandle = new ManualResetEvent(false);
            var overlapped = new Overlapped(0, 0, IntPtr.Zero, ar);
            if (userCallback != null)
            {
                overlappedPtr = overlapped.Pack(_ioCallback, buffer);
            }
            else
            {
                overlappedPtr = overlapped.UnsafePack(null, buffer);
            }
            ar._overlapped = overlappedPtr;
            if (CanSeek)
            {
                var length = Length;
                if (_exposedHandle)
                {
                    VerifyOSHandlePosition();
                }
                if ((_pos + numBytes) > length)
                {
                    SetLengthCore(_pos + numBytes);
                }
                overlappedPtr->OffsetLow = (int)_pos;
                overlappedPtr->OffsetHigh = (int)(_pos >> 0x20);
                SeekCore(numBytes, SeekOrigin.Current);
            }
            int hr;
            if ((WriteFileNative(_handle, buffer, offset, numBytes, overlappedPtr, out hr) == -1) && (numBytes != -1))
            {
                if (hr == ERROR_NO_DATA)
                {
                    ar.CallUserCallback();
                    return ar;
                }
                if (hr == ERROR_IO_PENDING)
                {
                    return ar;
                }
                if (!_handle.IsClosed && CanSeek)
                {
                    SeekCore(0, SeekOrigin.Current);
                }
                if (hr == ERROR_HANDLE_EOF)
                {
                    __Error.EndOfFile();
                    return ar;
                }
                __Error.WinIOError(hr, string.Empty);
            }
            return ar;
        }

        private unsafe AdvancedStreamAsyncResult BeginWriteFileGatherCore(
            IVirtualBuffer[] buffers, AsyncCallback userCallback, object stateObject)
        {
            // Prepare gather array
            var bufferCount = buffers.Length;
            var elemCount = buffers[0].BufferSize * bufferCount / VirtualBuffer.SystemPageSize;
            var elements =
                new SafeNativeMethods.FILE_SEGMENT_ELEMENT[elemCount + 1];
            for (var bufferIndex = 0; bufferIndex < bufferCount; ++bufferIndex)
            {
                var buffer = buffers[bufferIndex];

                // Add buffer to list in "SystemPageSize" sized chunks.
                var elemPerBuffer = buffer.BufferSize / VirtualBuffer.SystemPageSize;
                var elemOffset = elemPerBuffer * bufferIndex;
                for (var elemIndex = 0; elemIndex < elemPerBuffer; ++elemIndex)
                {
                    elements[elemOffset + elemIndex] = new SafeNativeMethods.FILE_SEGMENT_ELEMENT(
                        new IntPtr(((VirtualBuffer)buffer).Buffer + (VirtualBuffer.SystemPageSize * elemIndex)));
                }
            }

            // Calculate total number of pages and bytes to transfer
            var numBytes = elemCount * SystemPageSize;

            // Prepare async helper object
            var ar = new AdvancedStreamAsyncResult();
            ar._handle = _handle;
            ar._userCallback = userCallback;
            ar._userStateObject = stateObject;
            ar._isWrite = true;
            ar._isScatterGather = true;
            ar._numBufferedBytes = numBytes;
            var completeEvent = new ManualResetEvent(false);
            ar._waitHandle = completeEvent;

            // Ensure we don't get our buffers freed too early
            ar._pinnedBuffers = new GCHandle[buffers.Length];
            var index = 0;
            foreach (var buffer in buffers)
            {
                ar._pinnedBuffers[index++] = GCHandle.Alloc(buffer);
            }

            // Prepare overlapped structure
            NativeOverlapped* overlappedPtr;
            var overlapped = new Overlapped(0, 0, IntPtr.Zero, ar);
            if (userCallback != null)
            {
                overlappedPtr = overlapped.Pack(_ioCallback, elements);
            }
            else
            {
                overlappedPtr = overlapped.UnsafePack(null, elements);
            }
            ar._overlapped = overlappedPtr;

            int hr;
            if ((WriteFileGatherNative(elements, numBytes, overlappedPtr, out hr) == -1) && (numBytes != -1))
            {
                if (hr == ERROR_BROKEN_PIPE)
                {
                    overlappedPtr->InternalLow = IntPtr.Zero;
                    ar.CallUserCallback();
                    return ar;
                }
                if (hr == ERROR_IO_PENDING)
                {
                    return ar;
                }
                if (!_handle.IsClosed && CanSeek)
                {
                    SeekCore(0, SeekOrigin.Current);
                }
                if (hr == ERROR_HANDLE_EOF)
                {
                    __Error.EndOfFile();
                    return ar;
                }
                __Error.WinIOError(hr, string.Empty);
            }
            return ar;
        }

        private void FlushRead()
        {
            if ((_readPos - _readLen) != 0)
            {
                SeekCore(_readPos - _readLen, SeekOrigin.Current);
            }
            _readPos = 0;
            _readLen = 0;
        }

        private void FlushWrite(bool calledFromFinalizer)
        {
            if (_isAsync)
            {
                // Flush current buffered segment
                IAsyncResult asyncResult = BeginWriteCore(_buffer, 0, _writePos, null, null);
                if (!calledFromFinalizer)
                {
                    EndWrite(asyncResult);
                }
            }
            else
            {
                WriteCore(_buffer, 0, _writePos);
            }
            _writePos = 0;
        }

        private static SafeNativeMethods.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            SafeNativeMethods.SECURITY_ATTRIBUTES structure = null;
            if ((share & FileShare.Inheritable) != FileShare.None)
            {
                structure = new SafeNativeMethods.SECURITY_ATTRIBUTES();
                structure.nLength = Marshal.SizeOf(structure);
                structure.bInheritHandle = 1;
            }
            return structure;
        }

        private static unsafe SafeNativeMethods.SECURITY_ATTRIBUTES GetSecAttrs(
            FileShare share, FileSecurity fileSecurity, out object pinningHandle)
        {
            pinningHandle = null;
            SafeNativeMethods.SECURITY_ATTRIBUTES structure = null;
            if (((share & FileShare.Inheritable) != FileShare.None) || (fileSecurity != null))
            {
                structure = new SafeNativeMethods.SECURITY_ATTRIBUTES();
                structure.nLength = Marshal.SizeOf(structure);
                if ((share & FileShare.Inheritable) != FileShare.None)
                {
                    structure.bInheritHandle = 1;
                }
                if (fileSecurity == null)
                {
                    return structure;
                }
                var securityDescriptorBinaryForm = fileSecurity.GetSecurityDescriptorBinaryForm();
                pinningHandle = GCHandle.Alloc(securityDescriptorBinaryForm, GCHandleType.Pinned);
                fixed (byte* numRef = securityDescriptorBinaryForm)
                {
                    structure.pSecurityDescriptor = numRef;
                }
            }
            return structure;
        }

        private int ReadCore(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                IAsyncResult asyncResult = BeginReadCore(buffer, offset, count, null, null, 0);
                return EndRead(asyncResult);
            }
            if (_exposedHandle)
            {
                VerifyOSHandlePosition();
            }
            int hr, num2;
            unsafe
            {
                num2 = ReadFileNative(_handle, buffer, offset, count, null, out hr);
            }
            if (num2 == -1)
            {
                switch (hr)
                {
                    case ERROR_BROKEN_PIPE:
                        num2 = 0;
                        break;

                    case ERROR_INVALID_PARAMETER:
                        throw new ArgumentException("File handle not synchronised.");

                    default:
                        __Error.WinIOError(hr, string.Empty);
                        break;
                }
            }
            _pos += num2;
            return num2;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int hr)
        {
            if ((bytes.Length - offset) < count)
            {
                throw new IndexOutOfRangeException("Invalid offset or count - race condition detected.");
            }
            if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            bool result;
            var numBytesRead = 0;
            fixed (byte* numRef = bytes)
            {
                if (_isAsync)
                {
                    result = SafeNativeMethods.ReadFile(handle, numRef + offset, count, IntPtr.Zero, overlapped);
                }
                else
                {
                    result = SafeNativeMethods.ReadFile(handle, numRef + offset, count, out numBytesRead, IntPtr.Zero);
                }
            }

            if (!result)
            {
                hr = Marshal.GetLastWin32Error();
                if (((hr != ERROR_BROKEN_PIPE) && (hr != 0xe9)) && (hr == 6))
                {
                    _handle.SetHandleAsInvalid();
                }
                return -1;
            }

            hr = 0;
            return numBytesRead;
        }

        private unsafe int ReadFileScatterNative(SafeNativeMethods.FILE_SEGMENT_ELEMENT[] elements,
            int numBytes, NativeOverlapped* overlapped, out int hr)
        {
            // Check for zero length
            hr = 0;
            if (numBytes == 0)
            {
                return 0;
            }

            if (CanSeek)
            {
                var length = Length;
                if (_exposedHandle)
                {
                    VerifyOSHandlePosition();
                }
                if ((_pos + numBytes) > length)
                {
                    // TODO: If we are writable can be alter the length of
                    //	the file?
                    // For now - throw an exception.
                    throw new InvalidOperationException("Attempt to read past end of file.");
                }
                overlapped->OffsetLow = (int)_pos;
                overlapped->OffsetHigh = (int)(_pos >> 0x20);
                SeekCore(numBytes, SeekOrigin.Current);
            }

            if (!SafeNativeMethods.ReadFileScatter(_handle, elements,
                (uint)numBytes, IntPtr.Zero, overlapped))
            {
                hr = Marshal.GetLastWin32Error();
                if ((hr != ERROR_NO_DATA) && (hr == 6))
                {
                    _handle.SetHandleAsInvalid();
                }
                return -1;
            }
            return 0;
        }

        private long SeekCore(long offset, SeekOrigin origin)
        {
            int hr;
            var position = SafeNativeMethods.SetFilePointer(_handle, offset, origin, out hr);
            if (position == -1)
            {
                if (hr == 6)
                {
                    _handle.SetHandleAsInvalid();
                }
                __Error.WinIOError(hr, string.Empty);
            }
            _pos = position;
            return position;
        }

        private void SetLengthCore(long value)
        {
            // Save current offset
            var offset = _pos;

            // Verify position of any exposed handle
            if (_exposedHandle)
            {
                VerifyOSHandlePosition();
            }

            // Seek as necessary then adjust EOF position
            if (_pos != value)
            {
                SeekCore(value, SeekOrigin.Begin);
            }
            if (!SafeNativeMethods.SetEndOfFile(_handle))
            {
                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode == __Error.ERROR_INVALID_PARAMETER)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "file length too large.");
                }
                __Error.WinIOError(errorCode, string.Empty);
            }

            if (offset != value)
            {
                if (offset < value)
                {
                    SeekCore(offset, SeekOrigin.Begin);

                    if (_isSparse)
                    {
                        // Set increased file region as sparse area.
                        var zeroInfo =
                            new Win32.FILE_ZERO_DATA_INFORMATION();
                        zeroInfo.FileOffset = offset;
                        zeroInfo.BeyondFinalZero = value;
                        int bytesReturned;
                        Win32.SetZeroData(_handle, zeroInfo, out bytesReturned);
                    }
                }
                else
                {
                    SeekCore(0, SeekOrigin.End);
                }
            }
        }

        private void VerifyOSHandlePosition()
        {
            if (CanSeek)
            {
                var num = _pos;
                if (SeekCore(0, SeekOrigin.Current) != num)
                {
                    _readPos = 0;
                    _readLen = 0;
                    if (_writePos > 0)
                    {
                        _writePos = 0;
                        throw new IOException("Invalid handle position.");
                    }
                }
            }
        }

        private void WriteCore(byte[] buffer, int offset, int count)
        {
            if (_isAsync)
            {
                IAsyncResult asyncResult = BeginWriteCore(buffer, offset, count, null, null);
                EndWrite(asyncResult);
                return;
            }
            if (_exposedHandle)
            {
                VerifyOSHandlePosition();
            }
            int hr, bytesWritten;
            unsafe
            {
                bytesWritten = WriteFileNative(_handle, buffer, offset, count, null, out hr);
            }
            if (bytesWritten == -1)
            {
                switch (hr)
                {
                    case ERROR_NO_DATA:
                        bytesWritten = 0;
                        break;

                    case ERROR_INVALID_PARAMETER:
                        throw new IOException("File too long or handle not synchronised.");

                    default:
                        __Error.WinIOError(hr, string.Empty);
                        break;
                }
            }
            _pos += bytesWritten;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped* overlapped, out int hr)
        {
            if ((bytes.Length - offset) < count)
            {
                throw new IndexOutOfRangeException("Invalid offset or count - race condition detected.");
            }
            if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            var numBytesWritten = 0;
            bool result;
            fixed (byte* numRef = bytes)
            {
                if (_isAsync)
                {
                    result = SafeNativeMethods.WriteFile(handle, numRef + offset, count, IntPtr.Zero, overlapped);
                }
                else
                {
                    result = SafeNativeMethods.WriteFile(handle, numRef + offset, count, out numBytesWritten, IntPtr.Zero);
                }
            }
            if (!result)
            {
                hr = Marshal.GetLastWin32Error();
                if ((hr != ERROR_NO_DATA) && (hr == 6))
                {
                    _handle.SetHandleAsInvalid();
                }
                return -1;
            }

            hr = 0;
            return numBytesWritten;
        }

        private unsafe int WriteFileGatherNative(SafeNativeMethods.FILE_SEGMENT_ELEMENT[] elements,
            int numBytes, NativeOverlapped* overlapped, out int hr)
        {
            // Check for zero length
            hr = 0;
            if (numBytes == 0)
            {
                return 0;
            }

            if (CanSeek)
            {
                var length = Length;
                if (_exposedHandle)
                {
                    VerifyOSHandlePosition();
                }
                if ((_pos + numBytes) > length)
                {
                    SetLength(_pos + numBytes);
                }
                overlapped->OffsetLow = (int)_pos;
                overlapped->OffsetHigh = (int)(_pos >> 0x20);
                SeekCore(numBytes, SeekOrigin.Current);
            }

            if (!SafeNativeMethods.WriteFileGather(_handle, elements,
                (uint)numBytes, IntPtr.Zero, overlapped))
            {
                hr = Marshal.GetLastWin32Error();
                if ((hr != ERROR_NO_DATA) && (hr == 6))
                {
                    _handle.SetHandleAsInvalid();
                }
                return -1;
            }
            return 0;
        }
        #endregion
    }
}
