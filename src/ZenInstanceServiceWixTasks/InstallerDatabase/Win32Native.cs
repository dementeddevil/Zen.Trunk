using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    internal static class Win32Native
	{
		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiDatabaseOpenView(SafeMsiHandle database,
			[MarshalAs(UnmanagedType.LPTStr)] string query,
			[Out] out SafeMsiHandle viewHandle);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiViewExecute(SafeMsiHandle view, HandleRef record);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiViewFetch(SafeMsiHandle view,
			[In, Out] ref SafeMsiHandle record);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiViewModify(SafeMsiHandle view,
			[MarshalAs(UnmanagedType.I4)] ViewModify modifyFlags,
			SafeMsiHandle record);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiViewClose(SafeMsiHandle view);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern Int32 MsiOpenDatabase(
			[MarshalAs(UnmanagedType.LPTStr)] string databasePath,
			IntPtr persist, [Out] out SafeMsiHandle handle);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern Int32 MsiCloseHandle(IntPtr handle);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern SafeMsiHandle MsiGetLastErrorRecord();

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiDatabaseGenerateTransform(
			SafeMsiHandle database, SafeMsiHandle databaseReference,
			[MarshalAs(UnmanagedType.LPTStr)] string transformFile,
			int reserved1, int reserved2);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiCreateTransformSummaryInfo(
			SafeMsiHandle databaseHandle,
			SafeMsiHandle databaseReference,
			[MarshalAs(UnmanagedType.LPTStr)] string transformFile,
			[MarshalAs(UnmanagedType.I4)] TransformError errorConditions,
			[MarshalAs(UnmanagedType.I4)] TransformValidation validation);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiGetSummaryInformation(
			SafeMsiHandle databaseHandle,
			[MarshalAs(UnmanagedType.LPTStr)] string databasePath,
			int uiUpdateCount,
			[Out] out SafeMsiHandle summaryHandle);

		[DllImport("msi.dll",
			EntryPoint = "MsiGetPropertyCount",
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiGetPropertyCount(
			SafeMsiHandle summaryHandle, out int uiPropertyCount);

		[DllImport("msi.dll",
			EntryPoint = "MsiSummaryInfoSetPropertyA",
			CharSet = CharSet.Ansi, ExactSpelling = true,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiSummaryInfoSetProperty(
			SafeMsiHandle summaryHandle, int propertyId, int dataType, int intValue,
			IntPtr fileTimeValue,
			[MarshalAs(UnmanagedType.LPStr)] string textValue);

		[DllImport("msi.dll",
			EntryPoint = "MsiSummaryInfoGetPropertyA",
			CharSet = CharSet.Ansi,
			ExactSpelling = true,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiSummaryInfoGetProperty(
			SafeMsiHandle summaryHandle, int propertyId,
			[In, Out] ref IntPtr dataType,
			[In, Out] ref IntPtr intValue,
			[In, Out] ref IntPtr fileTimeValue,
			[In, Out, MarshalAs(UnmanagedType.LPStr)] StringBuilder textValue,
			[In, Out] ref IntPtr bufferSize);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiSummaryInfoPersist(
			SafeMsiHandle summaryHandle);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiDatabaseCommit(
			SafeMsiHandle database);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern SafeMsiHandle MsiCreateRecord(int paramCount);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern bool MsiRecordIsNull(SafeMsiHandle record,
			int fieldIndex);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordDataSize(SafeMsiHandle record,
			int fieldIndex);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordSetInteger(SafeMsiHandle record,
			int fieldIndex, int value);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordSetString(SafeMsiHandle record,
			int fieldIndex, [MarshalAs(UnmanagedType.LPTStr)] string value);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordGetInteger(SafeMsiHandle record,
			int fieldIndex);

		[DllImport("msi.dll", CharSet = CharSet.Auto)]
		public static extern int MsiRecordGetString(SafeMsiHandle record,
			int fieldIndex,
			StringBuilder value,
			ref int bufferSize);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordSetStream(SafeMsiHandle record, int fieldIndex,
			[MarshalAs(UnmanagedType.LPTStr)] string filePath);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordReadStream(SafeMsiHandle record, int fieldIndex,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] buffer,
			[In, Out] ref int bufferSize);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordClearData(SafeMsiHandle record);

		[DllImport("msi.dll", CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiRecordGetFieldCount(SafeMsiHandle record);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiFormatRecord(SafeMsiHandle install,
			SafeMsiHandle record,
			[MarshalAs(UnmanagedType.LPTStr, SizeParamIndex = 3)] StringBuilder result,
			[In, Out] ref int resultSize);

		[DllImport("msi.dll", CharSet = CharSet.Auto,
			CallingConvention = CallingConvention.Winapi)]
		public static extern int MsiVerifyPackage(
			[MarshalAs(UnmanagedType.LPTStr)] string packagePath);
	}
}
