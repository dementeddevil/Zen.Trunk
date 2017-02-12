using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Zen.Trunk.VirtualMemory
{
    internal static class __Error
    {
        // Fields
        internal const int ERROR_ACCESS_DENIED = 5;
        internal const int ERROR_FILE_NOT_FOUND = 2;
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_PATH_NOT_FOUND = 3;

        // Methods
        internal static void EndOfFile()
        {
            throw new EndOfStreamException("Beyond EOF.");
        }

        internal static void EndReadCalledTwice()
        {
            throw new ArgumentException("EndRead called twice!");
        }

        internal static void EndWriteCalledTwice()
        {
            throw new ArgumentException("EndWrite called twice!");
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, "File not open - stream disposed.");
        }

        internal static string GetDisplayablePath(string path, bool isInvalidPath)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var flag = false;
                if (path.Length < 2)
                {
                    return path;
                }
                if (path[0] == Path.DirectorySeparatorChar &&
                    path[1] == Path.DirectorySeparatorChar)
                {
                    flag = true;
                }
                else if (path[1] == Path.VolumeSeparatorChar)
                {
                    flag = true;
                }
                if (!flag && !isInvalidPath)
                {
                    return path;
                }
                var flag2 = false;
                try
                {
                    if (!isInvalidPath)
                    {
                        new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new[] { path }).Demand();
                        flag2 = true;
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (NotSupportedException)
                {
                }
                catch (SecurityException)
                {
                }
                if (flag2)
                {
                    return path;
                }
                if (path[path.Length - 1] == Path.DirectorySeparatorChar)
                {
                    path = "no permission for directory name.";
                    return path;
                }
                path = Path.GetFileName(path);
            }
            return path;
        }

        internal static void MemoryStreamNotExpandable()
        {
            throw new NotSupportedException("Stream not expandable.");
        }

        internal static void ReaderClosed()
        {
            throw new ObjectDisposedException(null, "Reader closed.");
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException("Reading not support.");
        }

        internal static void SeekNotSupported()
        {
            throw new NotSupportedException("Seeking not supported.");
        }

        internal static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, "Stream is closed.");
        }

        internal static void WinIODriveError(string driveName)
        {
            var errorCode = Marshal.GetLastWin32Error();
            WinIODriveError(driveName, errorCode);
        }

        internal static void WinIODriveError(string driveName, int errorCode)
        {
            switch (errorCode)
            {
                case 3:
                case 15:
                    throw new DriveNotFoundException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Drive {0} not found", new object[] { driveName }));
            }
            WinIOError(errorCode, driveName);
        }

        internal static void WinIOError()
        {
            WinIOError(Marshal.GetLastWin32Error(), string.Empty);
        }

        internal static void WinIOError(int errorCode, string maybeFullPath)
        {
            var isInvalidPath = (errorCode == 0x7b) || (errorCode == 0xa1);
            var fileName = GetDisplayablePath(maybeFullPath, isInvalidPath);
            switch (errorCode)
            {
                case 0x20:
                    if (fileName.Length == 0)
                    {
                        throw new IOException("No filename",
                            SafeNativeMethods.MakeHRFromErrorCode(errorCode));
                    }
                    throw new IOException(string.Format(
                            "Sharing violation on {0}", new object[] { fileName }),
                        SafeNativeMethods.MakeHRFromErrorCode(errorCode));

                case 80:
                    if (fileName.Length != 0)
                    {
                        throw new IOException(string.Format(
                                CultureInfo.InvariantCulture, "File exists {0}",
                                new object[] { fileName }),
                            SafeNativeMethods.MakeHRFromErrorCode(errorCode));
                    }
                    break;

                case 2:
                    if (fileName.Length == 0)
                    {
                        throw new FileNotFoundException("IO.FileNotFound");
                    }
                    throw new FileNotFoundException(string.Format(
                            CultureInfo.InvariantCulture, "File not found {0}", new object[] { fileName }),
                        fileName);

                case 3:
                    if (fileName.Length == 0)
                    {
                        throw new DirectoryNotFoundException("No path name");
                    }
                    throw new DirectoryNotFoundException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Path not found {0}.", new object[] { fileName }));

                case 5:
                    if (fileName.Length == 0)
                    {
                        throw new UnauthorizedAccessException("No path name - access denied.");
                    }
                    throw new UnauthorizedAccessException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Unauthorised access {0}.", new object[] { fileName }));

                case 15:
                    throw new DriveNotFoundException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Drive not found {0}", new object[] { fileName }));

                case ERROR_INVALID_PARAMETER:
                    throw new IOException(
                        SafeNativeMethods.GetMessage(errorCode),
                        SafeNativeMethods.MakeHRFromErrorCode(errorCode));

                case 0xb7:
                    if (fileName.Length != 0)
                    {
                        throw new IOException(string.Format(
                                CultureInfo.InvariantCulture,
                                "File already exists {0}.", new object[] { fileName }),
                            SafeNativeMethods.MakeHRFromErrorCode(errorCode));
                    }
                    break;

                case 0xce:
                    throw new PathTooLongException("IO.PathTooLong");

                case 0x3e3:
                    throw new OperationCanceledException();
            }
            throw new IOException(
                SafeNativeMethods.GetMessage(errorCode),
                SafeNativeMethods.MakeHRFromErrorCode(errorCode));
        }

        internal static void WriteNotSupported()
        {
            throw new NotSupportedException("NotSupported_UnwritableStream");
        }

        internal static void WriterClosed()
        {
            throw new ObjectDisposedException(null, "ObjectDisposed_WriterClosed");
        }

        internal static void WrongAsyncResult()
        {
            throw new ArgumentException("Arg_WrongAsyncResult");
        }

        internal static void ScatterGatherNotEnabled()
        {
            throw new NotSupportedException("Scatter/gather IO not enabled.");
        }

        internal static void NotAllowedWhenSystemBufferDisabled()
        {
            throw new InvalidOperationException("Cannot use this entry-point when system buffering has been disabled.");
        }
    }
}