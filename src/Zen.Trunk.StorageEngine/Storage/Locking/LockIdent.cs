// -----------------------------------------------------------------------
// <copyright file="LockIdent.cs" company="Zen Design Corp">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Zen.Trunk.Storage.Locking
{
	/// <summary>
	/// <c>LockIdent</c> contains helper methods for obtaining keys needed to
	/// access lock objects protecting various database resources.
	/// </summary>
	internal static class LockIdent
	{
		/// <summary>
		/// Gets the key for accessing the given database.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <returns></returns>
		public static string GetDatabaseKey(ushort dbId)
		{
			return string.Format("DBK:{0:X}", dbId);
		}

		/// <summary>
		/// Gets a key for accessing the root page of a given device.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="fileGroupId">The file group id.</param>
		/// <returns></returns>
		public static string GetFileGroupRootKey(ushort dbId, byte fileGroupId)
		{
			return string.Format("FRK:{0:X}${1:X}", dbId, fileGroupId);
		}

		/// <summary>
		/// Gets a key for accessing a distribution page.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <returns></returns>
		public static string GetDistributionKey(ushort dbId, ulong virtualPageId)
		{
			return string.Format("DLK:{0:X}${1:X}", dbId, virtualPageId);
		}

		/// <summary>
		/// Gets a key for accessing distribution page extent information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <param name="extentIndex">Index of the extent.</param>
		/// <returns></returns>
		public static string GetExtentLockKey(ushort dbId, ulong virtualPageId, uint extentIndex)
		{
			return string.Format("ELK:{0:X}${1:X}${2}", dbId, virtualPageId, extentIndex);
		}

		/// <summary>
		/// Gets a key for accessing index page information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="indexId">The index id.</param>
		/// <returns></returns>
		public static string GetIndexKey(ushort dbId, uint indexId)
		{
			return string.Format("IK:{0:X}${1:X}", dbId, indexId);
		}

		/// <summary>
		/// Gets a key for accessing the root definition of an index.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="indexId">The index id.</param>
		/// <returns></returns>
		public static string GetIndexRootKey(ushort dbId, uint indexId)
		{
			return string.Format("IRK:{0:X}${1:X}", dbId, indexId);
		}

		/// <summary>
		/// Gets a key for accessing a non-root and non-leaf index page.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="indexId">The index id.</param>
		/// <param name="logicalId">The logical id.</param>
		/// <returns></returns>
		public static string GetIndexInternalKey(ushort dbId, uint indexId, ulong logicalId)
		{
			return string.Format("IIK:{0:X}${1:X}${2:X}", dbId, indexId, logicalId);
		}

		/// <summary>
		/// Gets a key for accessing the leaf index page information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="indexId">The index id.</param>
		/// <param name="logicalId">The logical id.</param>
		/// <returns></returns>
		public static string GetIndexLeafKey(ushort dbId, uint indexId, ulong logicalId)
		{
			return string.Format("ILK:{0:X}${1:X}${2:X}", dbId, indexId, logicalId);
		}

		/// <summary>
		/// Gets a key for accessing object information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <returns></returns>
		public static string GetObjectLockKey(ushort dbId, uint objectId)
		{
			return string.Format("OLK:{0:X}${1:X}", dbId, objectId);
		}

		/// <summary>
		/// Gets a key for accessing table/sample schema information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <returns></returns>
		public static string GetSchemaLockKey(ushort dbId, uint objectId)
		{
			return string.Format("OSK:{0:X}${1:X}", dbId, objectId);
		}

		/// <summary>
		/// Gets a key for accessing table/sample data pages.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <param name="logicalId">The logical id.</param>
		/// <returns></returns>
		public static string GetDataLockKey(ushort dbId, uint objectId, ulong logicalId)
		{
			return string.Format("ODK:{0:X}${1:X}${2:X}", dbId, objectId, logicalId);
		}
	}
}
