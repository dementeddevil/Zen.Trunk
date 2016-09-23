using System;
using System.Collections.Generic;
using System.IO;

namespace Zen.Tasks.Wix.InstanceService.InstallerDatabase
{
    /// <summary>
	/// MsiDatabase provides a managed wrapper on a MSI database.
	/// </summary>
	public class MsiDatabase : IDisposable
	{
		#region Private Fields
	    private string _databasePathName;
		private PersistMode _persistMode;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiDatabase"/> class.
		/// </summary>
		public MsiDatabase()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MsiDatabase"/> class.
		/// </summary>
		/// <param name="databasePathName">Name of the database path.</param>
		/// <param name="persist">The persist.</param>
		public MsiDatabase(string databasePathName, PersistMode persist)
		{
			Open(databasePathName, persist);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MsiDatabase"/> class.
		/// </summary>
		/// <param name="inputDatabasePathName">Name of the input database path.</param>
		/// <param name="outputDatabasePathName">Name of the output database path.</param>
		/// <param name="persist">The persist.</param>
		public MsiDatabase(string inputDatabasePathName,
			string outputDatabasePathName, PersistMode persist)
		{
			if (!string.Equals(inputDatabasePathName, outputDatabasePathName))
			{
				if (File.Exists(outputDatabasePathName))
				{
					File.Delete(outputDatabasePathName);
				}
				File.Copy(inputDatabasePathName, outputDatabasePathName);
			}

			Open(outputDatabasePathName, persist);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Opens the specified database path name.
		/// </summary>
		/// <param name="databasePathName">Name of the database path.</param>
		/// <param name="persist">The persist.</param>
		public void Open(string databasePathName, PersistMode persist)
		{
		    Handle?.Dispose();

		    // Cache name and persist mode so we can do reopen later
			_databasePathName = databasePathName;
			_persistMode = persist;

			// Go ahead and open the DB
			Handle = Win32.MsiOpenDatabase(databasePathName, persist);
		}

		/// <summary>
		/// Commits this instance.
		/// </summary>
		public void Commit()
		{
			if (Handle != null)
			{
				Win32.MsiDatabaseCommit(Handle);
			}
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		public void Close()
		{
			if (Handle != null)
			{
				Handle.Dispose();
				Handle = null;
			}
		}

	    /// <summary>
		/// Commits and verifies this instance.
		/// </summary>
		public void CommitAndVerify()
		{
			Verify(true);
		}

		/// <summary>
		/// Performs an optional commit prior to verifying this instance.
		/// </summary>
		/// <param name="commit">if set to <c>true</c> then the instance is 
		/// committed before verification is performed.</param>
		public void Verify(bool commit = false)
		{
			// If database is open then close it temporarily
			var needReopen = false;
			if (Handle != null && !Handle.IsInvalid &&
				!Handle.IsClosed)
			{
				if (commit)
				{
					Commit();
				}

				Close();
				needReopen = true;
			}

			// Verify the database file.
			Win32.MsiVerifyPackage(_databasePathName);

			if (needReopen)
			{
				Open(_databasePathName, _persistMode);
			}
		}

		/// <summary>
		/// Executes the specified query against this instance.
		/// </summary>
		/// <param name="query">The query.</param>
		public void ExecuteNonQuery(string query)
		{
			using (var view = OpenView(query))
			{
				view.Execute(null);
			}
		}

		/// <summary>
		/// Inserts the specified file into the database Binary table under
		/// the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="filePath">The file path.</param>
		public void InsertBinaryStream(string name, string filePath)
		{
			using (var view = OpenView($"INSERT INTO `Binary` (`Name`,`Data`) VALUES ('{name}',?)"))
			{
				// Create stream record
				var record = new MsiRecord(1);
				record[1].SetStream(filePath);

				// Execute insert using stream record
				view.Execute(record);
			}
		}

		/// <summary>
		/// Inserts the specified file into the database _Storages table under
		/// the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="filePath">The file path.</param>
		public void InsertStorageStream(string name, string filePath)
		{
			using (var view = OpenView($"INSERT INTO `_Storages` (`Name`,`Data`) VALUES ('{name}',?)"))
			{
				// Create stream record
				var record = new MsiRecord(1);
				record[1].SetStream(filePath);

				// Execute insert using stream record
				view.Execute(record);
			}
		}

		/// <summary>
		/// Updates the directory table.
		/// </summary>
		/// <param name="directoryName">Name of the directory.</param>
		/// <param name="defaultDir">The default dir.</param>
		public void UpdateDirectoryTable(string directoryName, string defaultDir)
		{
			using (var view = OpenView($"UPDATE `Directory` SET `Directory`.`DefaultDir`=? WHERE `Directory`.`Directory`=?"))
			{
				var record = new MsiRecord(2);
				record[1].SetString(defaultDir);
				record[2].SetString(directoryName);
				view.Execute(record);
			}
		}

		/// <summary>
		/// Gets the property value.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		public string GetPropertyValue(string propertyName)
		{
			var result = string.Empty;
			using (var view = OpenView("SELECT `Property`.`Value` FROM `Property` WHERE `Property`.`Property`=?"))
			{
				var record = new MsiRecord(1);
				record[1].SetString(propertyName);
				view.Execute(record);

				var resultRow = new MsiRecord(1);
				if (view.Fetch(resultRow))
				{
					result = resultRow[1].GetString();
				}
			}
			return result;
		}

		/// <summary>
		/// Gets the indirect property value.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns></returns>
		public string GetPropertyValueIndirect(string propertyName)
		{
			var strIndirect = GetPropertyValue(propertyName);
			if (string.IsNullOrEmpty(strIndirect))
			{
				throw new ArgumentException(
                    $"Indirect argument [{propertyName}] not found.",
					nameof(propertyName));
			}
			return GetPropertyValue(strIndirect);
		}

		private class UpgradeInfo
		{
			public string VersionMin
			{
				get;
				set;
			}
			public string VersionMax
			{
				get;
				set;
			}
			public string Language
			{
				get;
				set;
			}
			public int Attributes
			{
				get;
				set;
			}
			public string Remove
			{
				get;
				set;
			}
			public string ActionProperty
			{
				get;
				set;
			}
		}

        /// <summary>
        /// Changes the upgrade code.
        /// </summary>
        /// <param name="newUpgradeCode">The new upgrade code.</param>
        public void ChangeUpgradeCode(string newUpgradeCode)
		{
			var oldUpgradeCode = GetPropertyValue("UpgradeCode");
			UpdatePropertyTable("UpgradeCode", newUpgradeCode.ToUpper());

			var upgradeList = new List<UpgradeInfo>();
			using (var readOldView = OpenView(
				"SELECT `VersionMin`,`VersionMax`,`Language`,`Attributes`,`Remove`,`ActionProperty` FROM `Upgrade` WHERE `Upgrade`.`UpgradeCode`=?"))
			{
				var readRecord = new MsiRecord(1);
				readRecord[1].SetString(oldUpgradeCode);
				readOldView.Execute(readRecord);

				var resultset = new MsiRecord(6);
				while (readOldView.Fetch(resultset))
				{
					var info =
						new UpgradeInfo
						{
							VersionMin = resultset[1].GetString(),
							VersionMax = resultset[2].GetString(),
							Language = resultset[3].GetString(),
							Attributes = resultset[4].GetInt(),
							Remove = resultset[5].GetString(),
							ActionProperty = resultset[6].GetString()
						};
					upgradeList.Add(info);
				}
			}

			using (var view = OpenView(
				"INSERT INTO `Upgrade` (`UpgradeCode`,`VersionMin`,`VersionMax`,`Language`,`Attributes`,`Remove`,`ActionProperty`) VALUES (?,?,?,?,?,?,?)"))
			{
				foreach (var info in upgradeList)
				{
					var insertRecord = new MsiRecord(7);
					insertRecord[1].SetString(newUpgradeCode.ToUpper());
					insertRecord[2].SetString(info.VersionMin);
					insertRecord[3].SetString(info.VersionMax);
					insertRecord[4].SetString(info.Language);
					insertRecord[5].SetInt(info.Attributes);
					insertRecord[6].SetString(info.Remove);
					insertRecord[7].SetString(info.ActionProperty);
					view.Execute(insertRecord);
				}
			}

			using (var view = OpenView(
				"DELETE FROM `Upgrade` WHERE `Upgrade`.`UpgradeCode`=?"))
			{
				var deleteRecord = new MsiRecord(1);
				deleteRecord[1].SetString(oldUpgradeCode.ToUpper());
				view.Execute(deleteRecord);
			}
		}

		/// <summary>
		/// Updates the property with the specified name to the specified
		/// value.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="propertyValue">The property value.</param>
		public void UpdatePropertyTable(string propertyName, string propertyValue)
		{
			using (var view = OpenView(
				"UPDATE `Property` SET `Property`.`Value`=? WHERE `Property`.`Property`=?"))
			{
				var record = new MsiRecord(2);
				record[1].SetString(propertyValue);
				record[2].SetString(propertyName);
				view.Execute(record);
			}
		}

		/// <summary>
		/// Inserts the property with the specified name and value into the
		/// property table.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="propertyValue">The property value.</param>
		public void InsertPropertyTable(string propertyName, string propertyValue)
		{
			using (var view = OpenView(
				"INSERT INTO `Property` (`Property`.`Property`,`Property`.`Value`) VALUES (?,?)"))
			{
				var record = new MsiRecord(2);
				record[1].SetString(propertyName);
				record[2].SetString(propertyValue);
				view.Execute(record);
			}
		}

		/// <summary>
		/// Executes the specified query and returns a view of the resultset.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns>A <see cref="MsiView"/> representing the query.</returns>
		public MsiView OpenView(string query)
		{
			var view = Win32.MsiDatabaseOpenView(Handle, query);
			return new MsiView(view);
		}

		/// <summary>
		/// Generates the transform.
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// <param name="transformFile">The transform file.</param>
		public void GenerateTransform(MsiDatabase reference, string transformFile)
		{
			Win32.MsiDatabaseGenerateTransform(Handle, reference.Handle, transformFile);
		}

		/// <summary>
		/// Creates the transform summary info.
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// <param name="transformFile">The transform file.</param>
		/// <param name="errorConditions">The error conditions.</param>
		/// <param name="validation">The validation.</param>
		public void CreateTransformSummaryInfo(MsiDatabase reference,
			string transformFile, TransformError errorConditions,
			TransformValidation validation)
		{
			Win32.MsiCreateTransformSummaryInfo(Handle, reference.Handle,
				transformFile, errorConditions, validation);
		}

	    /// <summary>
		/// Gets the summary information.
		/// </summary>
		/// <param name="updateCount">The update count.</param>
		/// <param name="databasePath">The database path.</param>
		/// <returns></returns>
		public MsiSummaryInformation GetSummaryInformation(int updateCount = 0, string databasePath = null)
		{
			var summary = Win32.MsiGetSummaryInformation(Handle, databasePath, updateCount);
			return new MsiSummaryInformation(summary);
		}
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Finalizes an instance of the <see cref="MsiDatabase"/> class.
        /// </summary>
        ~MsiDatabase()
		{
			Dispose(false);
		}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

        /// <summary>
        /// Gets the handle.
        /// </summary>
        /// <value>
        /// The handle.
        /// </value>
        protected internal SafeMsiHandle Handle { get; private set; }

	    /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
			    Handle?.Dispose();
			}
			Handle = null;
		}
		#endregion
	}
}
