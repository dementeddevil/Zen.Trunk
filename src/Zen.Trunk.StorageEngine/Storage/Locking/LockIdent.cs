// -----------------------------------------------------------------------
// <copyright file="LockIdent.cs" company="Zen Design Software">
// © Zen Design Software 2009 - 2016
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
		public static string GetDatabaseKey(DatabaseId dbId)
		{
			return $"DBK:{dbId.Value:X4}";
		}

		/// <summary>
		/// Gets a key for accessing the root page of a given device.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="fileGroupId">The file group id.</param>
		/// <returns></returns>
		public static string GetFileGroupRootKey(DatabaseId dbId, FileGroupId fileGroupId)
		{
			return $"FRK:{dbId.Value:X4}${fileGroupId.Value:X2}";
		}

		/// <summary>
		/// Gets a key for accessing a distribution page.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <returns></returns>
		public static string GetDistributionKey(DatabaseId dbId, VirtualPageId virtualPageId)
		{
			return $"DLK:{dbId.Value:X4}${virtualPageId.Value:X16}";
		}

		/// <summary>
		/// Gets a key for accessing distribution page extent information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="virtualPageId">The virtual page id.</param>
		/// <param name="extentIndex">Index of the extent.</param>
		/// <returns></returns>
		public static string GetExtentLockKey(DatabaseId dbId, VirtualPageId virtualPageId, uint extentIndex)
		{
			return $"ELK:{dbId.Value:X4}${virtualPageId.Value:X16}${extentIndex}";
		}

	    /// <summary>
	    /// Gets a key for accessing index page information.
	    /// </summary>
	    /// <param name="dbId">The db id.</param>
	    /// <param name="objectId">The object id.</param>
	    /// <param name="indexId">The index id.</param>
	    /// <returns></returns>
	    public static string GetIndexKey(DatabaseId dbId, ObjectId objectId, IndexId indexId)
		{
			return $"IK:{dbId.Value:X4}${objectId.Value:X8}${indexId.Value:X8}";
		}

	    /// <summary>
	    /// Gets a key for accessing the root definition of an index.
	    /// </summary>
	    /// <param name="dbId">The db id.</param>
	    /// <param name="objectId">The object id.</param>
	    /// <param name="indexId">The index id.</param>
	    /// <returns></returns>
	    public static string GetIndexRootKey(DatabaseId dbId, ObjectId objectId, IndexId indexId)
		{
			return $"IRK:{dbId.Value:X4}${objectId.Value:X8}${indexId.Value:X8}";
		}

	    /// <summary>
	    /// Gets a key for accessing a non-root and non-leaf index page.
	    /// </summary>
	    /// <param name="dbId">The db id.</param>
	    /// <param name="objectId">The object id.</param>
	    /// <param name="indexId">The index id.</param>
	    /// <param name="logicalId">The logical id.</param>
	    /// <returns></returns>
	    public static string GetIndexInternalKey(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId)
		{
			return $"IIK:{dbId.Value:X4}${objectId.Value:X8}${indexId.Value:X8}${logicalId.Value:X16}";
		}

        /// <summary>
        /// Gets a key for accessing the leaf index page information.
        /// </summary>
        /// <param name="dbId">The db id.</param>
        /// <param name="objectId">The object id.</param>
        /// <param name="indexId">The index id.</param>
        /// <param name="logicalId">The logical id.</param>
        /// <returns></returns>
        public static string GetIndexLeafKey(DatabaseId dbId, ObjectId objectId, IndexId indexId, LogicalPageId logicalId)
		{
			return $"ILK:{dbId.Value:X4}${objectId.Value:X8}${indexId.Value:X8}${logicalId.Value:X16}";
		}

		/// <summary>
		/// Gets a key for accessing object information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <returns></returns>
		public static string GetObjectLockKey(DatabaseId dbId, ObjectId objectId)
		{
			return $"OLK:{dbId.Value:X4}${objectId.Value:X8}";
		}

		/// <summary>
		/// Gets a key for accessing table/sample schema information.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <returns></returns>
		public static string GetSchemaLockKey(DatabaseId dbId, ObjectId objectId)
		{
			return $"OSK:{dbId.Value:X4}${objectId.Value:X8}";
		}

		/// <summary>
		/// Gets a key for accessing table/sample data pages.
		/// </summary>
		/// <param name="dbId">The db id.</param>
		/// <param name="objectId">The object id.</param>
		/// <param name="logicalId">The logical id.</param>
		/// <returns></returns>
		public static string GetDataLockKey(DatabaseId dbId, ObjectId objectId, LogicalPageId logicalId)
		{
			return $"ODK:{dbId.Value:X4}${objectId.Value:X8}${logicalId.Value:X16}";
		}
	}
}
