using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    internal static class Win32
	{
		public static SafeMsiHandle MsiDatabaseOpenView(
			SafeMsiHandle database, string query)
		{
			SafeMsiHandle viewHandle;
			var result = Win32Native.MsiDatabaseOpenView(
				database, query, out viewHandle);
			CheckMsiResult(result, "MsiDatabaseOpenView");
			return viewHandle;
		}

		public static void MsiViewExecute(SafeMsiHandle view,
			HandleRef record)
		{
			var result = Win32Native.MsiViewExecute(view, record);
			CheckMsiResult(result, "MsiViewExecute");
		}

		public static bool MsiViewFetch(SafeMsiHandle view, ref SafeMsiHandle record)
		{
			var result = Win32Native.MsiViewFetch(view, ref record);
			if (result == (int)WinError.ERROR_NO_MORE_ITEMS)
			{
				return false;
			}
			CheckMsiResult(result, "MsiViewFetch");
			return true;
		}

		public static void MsiViewModify(SafeMsiHandle view,
			ViewModify modifyFlags, SafeMsiHandle record)
		{
			var result = Win32Native.MsiViewModify(view, modifyFlags,
				record);
			CheckMsiResult(result, "MsiViewModify");
		}

		public static void MsiViewClose(SafeMsiHandle view)
		{
			var result = Win32Native.MsiViewClose(view);
			CheckMsiResult(result, "MsiViewClose");
		}

		public static SafeMsiHandle MsiOpenDatabase(string databasePath,
			PersistMode persist)
		{
			SafeMsiHandle database;
			var marshalPersist = new IntPtr((int)persist);
			var result = Win32Native.MsiOpenDatabase(databasePath, marshalPersist, out database);
			CheckMsiResult(result, "MsiOpenDatabase");
			return database;
		}

		public static SafeMsiHandle MsiGetLastErrorRecord()
		{
			return Win32Native.MsiGetLastErrorRecord();
		}

		public static void MsiDatabaseGenerateTransform(
			SafeMsiHandle database, SafeMsiHandle reference,
			string transformFile)
		{
			var result = Win32Native.MsiDatabaseGenerateTransform(
				database, reference, transformFile, 0, 0);
			CheckMsiResult(result, "MsiDatabaseGenerateTransform");
		}

		public static void MsiCreateTransformSummaryInfo(
			SafeMsiHandle database, SafeMsiHandle reference,
			string transformFile, TransformError errorConditions,
			TransformValidation validation)
		{
			var result = Win32Native.MsiCreateTransformSummaryInfo(
				database, reference, transformFile, errorConditions, validation);
			CheckMsiResult(result, "MsiCreateTransformationSummaryInfo");
		}

		public static SafeMsiHandle MsiGetSummaryInformation(SafeMsiHandle database,
			string databasePath, int updateCount)
		{
			SafeMsiHandle summaryHandle;
			var result = Win32Native.MsiGetSummaryInformation(database,
				databasePath, updateCount, out summaryHandle);
			CheckMsiResult(result, "MsiDatabaseCommit");
			return summaryHandle;
		}

		public static int MsiGetPropertyCount(SafeMsiHandle summary)
		{
			int propertyCount;
			var result = Win32Native.MsiGetPropertyCount(summary,
				out propertyCount);
			CheckMsiResult(result, "MsiGetPropertyCount");
			return propertyCount;
		}

		public static void MsiSummaryInfoSetProperty(SafeMsiHandle summary,
			SummaryProperty property, VarEnum dataType, Nullable<int> intValue,
			Nullable<System.Runtime.InteropServices.ComTypes.FILETIME> fileTimeValue,
			string textValue)
		{
			var result = 0;
			var marshalFileTime = new IntPtr();
			var fileHandle = new GCHandle();
			var fileHandleAllocated = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				if (fileTimeValue.HasValue)
				{
					fileHandle = GCHandle.Alloc(fileTimeValue.Value, GCHandleType.Pinned);
					fileHandleAllocated = true;
					marshalFileTime = fileHandle.AddrOfPinnedObject();
				}
				var marshalIntValue = 0;
				if (intValue.HasValue)
				{
					marshalIntValue = intValue.Value;
				}
				result = Win32Native.MsiSummaryInfoSetProperty(summary,
					(int)property, (int)dataType, marshalIntValue,
					marshalFileTime, textValue);
			}
			finally
			{
				if (fileHandleAllocated)
				{
					fileHandle.Free();
				}
			}
			CheckMsiResult(result, "MsiSummaryInfoSetProperty");
		}

		public static int MsiSummaryInfoGetProperty(SafeMsiHandle summary,
			SummaryProperty property, out VarEnum dataType, out int intValue,
			out System.Runtime.InteropServices.ComTypes.FILETIME fileTimeValue,
			out StringBuilder textValue, int bufferSize)
		{
			var result = 0;

			// Build marshalling FILETIME holder for FILETIME value
			fileTimeValue = new System.Runtime.InteropServices.ComTypes.FILETIME();
			IntPtr marshalDataType = new IntPtr(),
				marshalIntValue = new IntPtr(),
				marshalFileTime = new IntPtr(),
				marshalBufferSize = new IntPtr(bufferSize);
			var fileHandle = new GCHandle();
			var fileHandleAllocated = false;

			// Build marshalling string builder for text value
			var builder = new StringBuilder(bufferSize);

			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				fileHandle = GCHandle.Alloc(fileTimeValue, GCHandleType.Pinned);
				fileHandleAllocated = true;
				marshalFileTime = fileHandle.AddrOfPinnedObject();
				result = Win32Native.MsiSummaryInfoGetProperty(summary,
					(int)property, ref marshalDataType, ref marshalIntValue,
					ref marshalFileTime, builder, ref marshalBufferSize);
			}
			finally
			{
				if (fileHandleAllocated)
				{
					fileHandle.Free();
				}
			}
			CheckMsiResult(result, "MsiSummaryInfoGetProperty");
			dataType = (VarEnum)marshalDataType.ToInt32();
			intValue = marshalIntValue.ToInt32();
			if (dataType == VarEnum.VT_LPSTR ||
				dataType == VarEnum.VT_LPWSTR)
			{
				builder.Length = marshalBufferSize.ToInt32();
			}
			textValue = builder;
			return result;
		}

		public static void MsiSummaryInfoPersist(SafeMsiHandle summary)
		{
			var result = Win32Native.MsiSummaryInfoPersist(summary);
			CheckMsiResult(result, "MsiSummaryInfoPersist");
		}

		public static void MsiDatabaseCommit(SafeMsiHandle database)
		{
			var result = Win32Native.MsiDatabaseCommit(database);
			CheckMsiResult(result, "MsiDatabaseCommit");
		}

		public static SafeMsiHandle MsiCreateRecord(int paramCount)
		{
			return Win32Native.MsiCreateRecord(paramCount);
		}

		public static bool MsiRecordIsNull(SafeMsiHandle record,
			int fieldIndex)
		{
			return Win32Native.MsiRecordIsNull(record, fieldIndex);
		}

		public static int MsiRecordDataSize(SafeMsiHandle record,
			int fieldIndex)
		{
			return Win32Native.MsiRecordDataSize(record, fieldIndex);
		}

		public static void MsiRecordSetInteger(SafeMsiHandle record,
			int fieldIndex, int value)
		{
			var result = Win32Native.MsiRecordSetInteger(record,
				fieldIndex, value);
			CheckMsiResult(result, "MsiRecordSetInteger");
		}

		public static void MsiRecordSetString(SafeMsiHandle record,
			int fieldIndex, string value)
		{
			var result = Win32Native.MsiRecordSetString(record,
				fieldIndex, value);
			CheckMsiResult(result, "MsiRecordSetString");
		}

		public static int MsiRecordGetInteger(SafeMsiHandle record,
			int fieldIndex)
		{
			return Win32Native.MsiRecordGetInteger(record, fieldIndex);
		}

		public static string MsiRecordGetString(SafeMsiHandle record,
			int fieldIndex)
		{
			var builder = new StringBuilder();
			var result = 0;
			var bufferSize = 128;
			while (true)
			{
				builder.Capacity = bufferSize;
				result = Win32Native.MsiRecordGetString(record,
					fieldIndex, builder, ref bufferSize);

				// Increase buffer size as required
				if (result == (int)WinError.ERROR_MORE_DATA)
				{
					bufferSize += 1;
					continue;
				}

				// Check the result code and setup return buffer size
				CheckMsiResult(result, "MsiRecordGetString");
				builder.Length = bufferSize;
				break;
			}
			return builder.ToString();
		}

		public static void MsiRecordSetStream(SafeMsiHandle record,
			int fieldIndex, string filePath)
		{
			var result = Win32Native.MsiRecordSetStream(record,
				fieldIndex, filePath);
			CheckMsiResult(result, "MsiRecordSetStream");
		}

		public static int MsiRecordReadStream(SafeMsiHandle record,
			int fieldIndex, byte[] buffer, int bufferSize)
		{
			var result = 0;
			result = Win32Native.MsiRecordReadStream(record,
				fieldIndex, buffer, ref bufferSize);
			CheckMsiResult(result, "MsiRecordReadStream");
			return result;
		}

		public static void MsiRecordClearData(SafeMsiHandle record)
		{
			var result = Win32Native.MsiRecordClearData(record);
			CheckMsiResult(result, "MsiRecordClearData");
		}

		public static int MsiRecordGetFieldCount(SafeMsiHandle record)
		{
			return Win32Native.MsiRecordGetFieldCount(record);
		}

		public static void MsiFormatRecord(SafeMsiHandle install,
			SafeMsiHandle record, StringBuilder result)
		{
			int error, resultSize = 128;
			while (true)
			{
				// Prepare for secured call
				result.Capacity = resultSize;

				error = Win32Native.MsiFormatRecord(install, record,
					result, ref resultSize);

				// Deal with expanding the buffer
				if (error == (int)WinError.ERROR_MORE_DATA)
				{
					resultSize += 1;
					continue;
				}

				// Check the result code
				CheckMsiResult(error, "MsiFormatRecord");

				// Terminate
				result.Length = resultSize;
				break;
			}
		}

		public static void MsiVerifyPackage(string packagePath)
		{
			var result = Win32Native.MsiVerifyPackage(packagePath);
			CheckMsiResult(result, "MsiVerifyPackage");
		}

		private static void CheckMsiResult(int result, string method)
		{
			switch (result)
			{
				case (int)WinError.ERROR_SUCCESS:
					return;
				case (int)WinError.ERROR_INVALID_HANDLE:
					throw new ObjectDisposedException("handle",
						"Object handle already disposed in " + method);
				case (int)WinError.ERROR_INVALID_HANDLE_STATE:
					throw new InvalidOperationException(
						"Invalid handle state detected in " + method);
				case (int)WinError.ERROR_GEN_FAILURE:
					throw new InvalidProgramException(
						"General failure encountered in " + method);
				case (int)WinError.ERROR_ACCESS_DENIED:
					throw new SecurityException($"Access denied to {method}.");
				//case (int) WinError.ERROR_INSTALL_SERVICE_FAILURE:
				//	throw new 
				default:
					var lastErrorRecordHandle = Win32Native.MsiGetLastErrorRecord();
					if (lastErrorRecordHandle != null && !lastErrorRecordHandle.IsInvalid)
					{
						var resultBuffer = new StringBuilder(1024);
						var resultLength = 1024;
						Win32Native.MsiFormatRecord(new SafeMsiHandle(IntPtr.Zero, true), lastErrorRecordHandle, resultBuffer, ref resultLength);

						throw new Win32Exception(result, string.Format(
							"Unknown WIN32 error {2} occurred {0} in {1}.",
							result, method, resultBuffer.ToString()));
					}
					else
					{
						throw new Win32Exception(result, $"Unknown WIN32 error occurred {result} in {method}.");
					}
			}
		}

		private enum WinError
		{
			ERROR_SUCCESS = 0,
			ERROR_ACCESS_DENIED = 5,
			ERROR_INVALID_HANDLE = 6,
			ERROR_GEN_FAILURE = 31,
			ERROR_MORE_DATA = 234,
			ERROR_NO_MORE_ITEMS = 259,
			ERROR_INSTALL_SERVICE_FAILURE = 1601,
			ERROR_INSTALL_USEREXIT = 1602,
			ERROR_INSTALL_FAILURE = 1603,
			ERROR_INSTALL_SUSPEND = 1604,
			ERROR_UNKNOWN_PRODUCT = 1605,
			ERROR_UNKNOWN_FEATURE = 1606,
			ERROR_UNKNOWN_COMPONENT = 1607,
			ERROR_UNKNOWN_PROPERTY = 1608,
			ERROR_INVALID_HANDLE_STATE = 1609,
			ERROR_BAD_CONFIGURATION = 1610,
			ERROR_INDEX_ABSENT = 1611,
			ERROR_INSTALL_SOURCE_ABSENT = 1612,
			ERROR_INSTALL_PACKAGE_VERSION = 1613,
			ERROR_PRODUCT_UNINSTALLED = 1614,
			ERROR_BAD_QUERY_SYNTAX = 1615,
			ERROR_INVALID_FIELD = 1616,
			ERROR_DEVICE_REMOVED = 1617,
			ERROR_INSTALL_ALREADYRUNNING = 1618,
			ERROR_INSTALL_PACKAGE_OPEN_FAILED = 1619,
			ERROR_INSTALL_PACKAGE_INVALID = 1620,
			ERROR_INSTALL_UI_FAILURE = 1621,
			ERROR_INSTALL_LOG_FAILURE = 1622,
			ERROR_INSTALL_LANGUAGE_UNSUPPORTED = 1623
		}
	}
}
