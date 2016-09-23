using System;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Zen.Tasks.Wix.InstanceService.InstallerDatabase;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public abstract class MsiTransformPacker : IDisposable
	{
		#region Private Fields
		private string _workingFolder;
	    private string _outputDatabasePathName;
	    private List<MsiTransform> _transforms;
	    #endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiTransformPacker"/> class.
		/// </summary>
		/// <param name="keepFiles">
		/// if set to <c>true</c> [keep files].
		/// </param>
		protected MsiTransformPacker(bool keepFiles = true)
		{
			KeepFiles = keepFiles;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the transforms.
		/// </summary>
		/// <value>The transforms.</value>
		public IList<MsiTransform> Transforms => _transforms ?? (_transforms = new List<MsiTransform>());

	    /// <summary>
		/// Gets the output database.
		/// </summary>
		/// <value>The output database.</value>
		public MsiDatabase OutputDatabase { get; private set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public TaskLoggingHelper Logger { get; set; }
	    #endregion

		#region Public Methods
		/// <summary>
		/// Creates the transform database.
		/// </summary>
		/// <param name="inputDatabase">The input database.</param>
		/// <param name="outputDatabase">The output database.</param>
		/// <param name="workingFolder">The working folder.</param>
		/// <param name="upgradeCode">The upgrade code.</param>
		/// <param name="keepFiles">if set to <c>true</c> [keep files].</param>
		/// <returns></returns>
		public MsiDatabase CreateTransformDatabase(
			string inputDatabase,
			string outputDatabase,
			string workingFolder,
			Guid upgradeCode,
			bool keepFiles)
		{
			// Normalise database path
			//if (string.IsNullOrEmpty (Path.GetDirectoryName (inputDatabase)) &&
			//	string.IsNullOrEmpty (Path.GetPathRoot (inputDatabase)))
			//{
			//	inputDatabase = Path.Combine (Directory.GetCurrentDirectory (), inputDatabase);
			//}
			InputDatabasePathName = inputDatabase;
			_outputDatabasePathName = outputDatabase;
			_workingFolder = workingFolder;
			UpgradeCode = upgradeCode;
			KeepFiles = keepFiles;

			// Generate transformation objects
			DisposeTransforms();
			GenerateTransforms();

			// Create output database
			CreateOutputDatabase();

			// Package our transforms
			PackageTransforms();

			// Return the database object
			return OutputDatabase;
		}
		#endregion

		#region Protected Properties
		/// <summary>
		/// Gets the name of the input database path.
		/// </summary>
		/// <value>The name of the input database path.</value>
		protected string InputDatabasePathName { get; private set; }

	    /// <summary>
		/// Gets the upgrade code.
		/// </summary>
		/// <value>The upgrade code.</value>
		protected Guid UpgradeCode { get; private set; } = Guid.Empty;

	    /// <summary>
		/// Gets a value indicating whether [keep files].
		/// </summary>
		/// <value><c>true</c> if [keep files]; otherwise, <c>false</c>.</value>
		protected bool KeepFiles { get; private set; }
	    #endregion

		#region Protected Methods
		/// <summary>
		/// Generates the transforms.
		/// </summary>
		protected abstract void GenerateTransforms();

		/// <summary>
		/// Creates the output database.
		/// </summary>
		/// <remarks>
		/// <para>
		/// By default the output database is a straight copy of the input
		/// database and is opened in transact mode.
		/// </para>
		/// <para>
		/// The <see cref="P:Database"/> property is valid after this call.
		/// </para>
		/// </remarks>
		protected virtual void CreateOutputDatabase()
		{
			OutputDatabase = new MsiDatabase(InputDatabasePathName,
				_outputDatabasePathName, PersistMode.Transact);
		}

		/// <summary>
		/// Packages the transforms into the output database.
		/// </summary>
		protected virtual void PackageTransforms()
		{
			foreach (var transform in _transforms)
			{
				PackageTransform(transform);
			}
		}

		/// <summary>
		/// Packages the specified transform into the output database.
		/// </summary>
		/// <param name="transform">The transform to package.</param>
		/// <returns>
		/// Boolean <c>true</c> if the transform was successfully packaged;
		/// otherwise, <c>false</c>.
		/// </returns>
		protected abstract bool PackageTransform(MsiTransform transform);

		/// <summary>
		/// Packages the binary file into the output database.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="filePath">The file path.</param>
		protected virtual void PackageBinaryFile(string key, string filePath)
		{
			if (OutputDatabase == null)
			{
				throw new InvalidOperationException();
			}

			Logger?.LogMessage($"\tPackaging Binary table, [K:{key} F:{filePath}]");
			OutputDatabase.InsertBinaryStream(key, filePath);
			OutputDatabase.CommitAndVerify();
		}

		/// <summary>
		/// Packages the binary file into the output database.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="filePath">The file path.</param>
		protected virtual void PackageStorageFile(string key, string filePath)
		{
			if (OutputDatabase == null)
			{
				throw new InvalidOperationException();
			}

            Logger?.LogMessage($"\tPackaging Stream table, [K:{key} F:{filePath}]");
            OutputDatabase.InsertStorageStream(key, filePath);
			OutputDatabase.CommitAndVerify();
		}

		/// <summary>
		/// Creates the transform.
		/// </summary>
		/// <returns></returns>
		protected virtual MsiTransform CreateTransform()
		{
			return new MsiTransform(KeepFiles)
			{
			    Logger = Logger
			};
		}

		/// <summary>
		/// Opens the transform.
		/// </summary>
		/// <param name="tfm">The TFM.</param>
		protected virtual void OpenTransform(MsiTransform tfm)
		{
			tfm.Open(InputDatabasePathName, _workingFolder, "T");
		}

		/// <summary>
		/// Creates the and open transform.
		/// </summary>
		/// <returns></returns>
		protected MsiTransform CreateAndOpenTransform()
		{
			var tfm = CreateTransform();
			OpenTransform(tfm);
			return tfm;
		}
		#endregion

		#region Private Methods
		private void DisposeTransforms()
		{
			foreach (var transform in Transforms)
			{
				transform.Dispose();
			}
			Transforms.Clear();
		}
		#endregion

		#region IDisposable Members
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="MsiTransformPacker"/> is reclaimed by garbage collection.
		/// </summary>
		~MsiTransformPacker()
		{
			Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, 
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> to release both managed and unmanaged resources; 
		/// <c>false</c> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				DisposeTransforms();
			    OutputDatabase?.Dispose();
			}

			_transforms = null;
			OutputDatabase = null;
		}
		#endregion
	}
}
