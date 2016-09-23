using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Zen.Tasks.Wix.InstanceService.InstallerDatabase;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public class MsiTransform : IDisposable
	{
		#region Protected Objects
		protected class TransformTask
		{
		    public TransformTask(int taskIndex, string taskName, string detail)
			{
				TaskIndex = taskIndex;
				TaskName = taskName;
				Detail = detail;
			}

			public int TaskIndex { get; }

		    public string TaskName { get; }

		    public string Detail { get; }
		}
		#endregion

		#region Private Fields
		private string _workingFolder;

		private string _inputDatabasePathName;

	    private string _transformBaseName;
		private string _transformDatabasePathName;
	    private bool _keepFiles;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiTransform"/> class.
		/// </summary>
		/// <remarks>
		/// By default the transform will keep the resultant transform file.
		/// </remarks>
		public MsiTransform()
			: this(true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MsiTransform"/> class.
		/// </summary>
		/// <param name="keepFiles">if set to <c>true</c> [keep files].</param>
		public MsiTransform(bool keepFiles)
		{
			_keepFiles = keepFiles;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the input database.
		/// </summary>
		/// <value>The input database.</value>
		public MsiDatabase InputDatabase { get; private set; }

	    /// <summary>
		/// Gets the transform database.
		/// </summary>
		/// <value>The transform database.</value>
		public MsiDatabase TransformDatabase { get; private set; }

	    /// <summary>
		/// Gets the name of the transform path.
		/// </summary>
		/// <value>The name of the transform path.</value>
		public string TransformPathName { get; private set; }

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
		/// Opens the specified working folder.
		/// </summary>
		/// <param name="inputDatabasePathName">Name of the input database path.</param>
		/// <param name="workingFolder">The working folder.</param>
		/// <param name="transformBaseName">Name of the transform base.</param>
		public virtual void Open(string inputDatabasePathName,
			string workingFolder, string transformBaseName)
		{
			// Create instance folder
			_workingFolder = workingFolder;
			if (!Directory.Exists(_workingFolder))
			{
				Directory.CreateDirectory(_workingFolder);
			}

			// Construct our temporary file paths
			_inputDatabasePathName = inputDatabasePathName;
			var baseName = Path.GetFileNameWithoutExtension(_inputDatabasePathName);
			_transformBaseName = baseName + transformBaseName;
			_transformDatabasePathName = Path.Combine(_workingFolder,
			    $"{_transformBaseName}.msi");
			TransformPathName = Path.Combine(_workingFolder,
			    $"{_transformBaseName}.mst");

			// Ensure our destination files don't already exist
			LogMessage("Performing pre-clean...");
			if (File.Exists(TransformPathName))
			{
				File.Delete(TransformPathName);
			}

            // Now open each database
            LogMessage($"Opening reference database as read-only.\n\t{_inputDatabasePathName}");
			InputDatabase = new MsiDatabase(_inputDatabasePathName, PersistMode.ReadOnly);

            LogMessage($"Opening transform database as transacted.\n\t{_transformDatabasePathName}");
			TransformDatabase = new MsiDatabase(_inputDatabasePathName,
				_transformDatabasePathName, PersistMode.Direct);
		}

		/// <summary>
		/// Reopens the transform database and verify.
		/// </summary>
		public void ReopenTransformDatabaseAndVerify()
		{
			TransformDatabase.Verify();
		}

		/// <summary>
		/// Creates the transform.
		/// </summary>
		/// <param name="productCode">The product code.</param>
		/// <param name="upgradeCode">The upgrade code.</param>
		public void CreateTransform(Guid productCode, Guid upgradeCode)
		{
            // Process product code and upgrade code changes
            // TODO: Add version changes here too
            LogMessage($"Updating product code to {productCode:B}.");
			TransformDatabase.UpdatePropertyTable("ProductCode", productCode.ToString("B").ToUpper());
			if (upgradeCode != Guid.Empty)
			{
                LogMessage($"Updating upgrade code to {upgradeCode:B}.");
				TransformDatabase.ChangeUpgradeCode(upgradeCode.ToString("B").ToUpper());
			}

			// Hook into derived class transformations
			var taskIter = OnCreateTransform();
			if (taskIter != null)
			{
				var hasData = true;
				while (hasData)
				{
					if ((hasData = taskIter.MoveNext()) != false)
					{
						// Write trace information
						var task = taskIter.Current;
                        LogMessage($"\t{task.TaskIndex} sub-task {task.TaskName}\t{task.Detail}");

						// Commit and verify
						TransformDatabase.CommitAndVerify();
					}
				}
			}

			// Commit database changes
			LogMessage("Committing changes.");
			TransformDatabase.Commit();
		}

		/// <summary>
		/// Generates a transform from the changes made to the transformation
		/// database.
		/// </summary>
		public void GenerateTransform()
		{
            LogMessage($"Generating instance transform.\n\t{TransformPathName}");
			TransformDatabase.GenerateTransform(InputDatabase, TransformPathName);
		}

		/// <summary>
		/// Generates a transform from the changes made to the transformation
		/// database and creates transform summary information.
		/// </summary>
		public void GenerateTransform(TransformError errorConditions,
			TransformValidation validation)
		{
			GenerateTransform();
			try
			{
				/*using (MsiDatabase transform = new MsiDatabase (_transformPathName, PersistMode.Transact))
				{
					using (MsiSummaryInformation summary = _transformDatabase.GetSummaryInformation (10),
						refSummary = _originalDatabase.GetSummaryInformation ())
					{
						summary.SetInt16 (SummaryProperty.CodePage, 1033);
						summary.SetString (SummaryProperty.Title, "Transform");
						summary.SetString (SummaryProperty.Subject, refSummary.GetString (SummaryProperty.Subject, 255));
						summary.SetString (SummaryProperty.Author, "System Instance Packager");
						summary.SetString (SummaryProperty.Keywords, "Installer Transform Jems Service Instance " + _instanceFileSuffix);
						summary.SetString (SummaryProperty.Template, "Intel;1033");
						summary.SetString (SummaryProperty.RevisionNumber, string.Format (
							"{0} {1};{2} {3};{4}",
							_originalDatabase.GetPropertyValue ("ProductCode", 255),
							_originalDatabase.GetPropertyValue ("ProductVersion", 255),
							_transformDatabase.GetPropertyValue ("ProductCode", 255),
							_transformDatabase.GetPropertyValue ("ProductVersion", 255),
							_transformDatabase.GetPropertyValue ("UpgradeCode", 255)));
						summary.SetInt32 (SummaryProperty.PageCount, 310);
						summary.SetInt32 (SummaryProperty.CharacterCount,
							(((int) errorConditions) & 0xffff) |
							((((int) validation) & 0xffff) << 16));
						summary.SetString (SummaryProperty.CreatingApplication,
							"Zen Instance Transform Generator");
						try
						{
							summary.SetInt32 (SummaryProperty.Security, 4);
						}
						catch
						{
						}
						summary.Persist ();
					}
					transform.Commit ();
				}*/
				TransformDatabase.CreateTransformSummaryInfo(
                    InputDatabase, TransformPathName, errorConditions, validation);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Closes this instance.
		/// </summary>
		public void Close()
		{
			if (InputDatabase != null)
			{
				InputDatabase.Dispose();
				InputDatabase = null;
			}
			if (TransformDatabase != null)
			{
				TransformDatabase.Dispose();
				TransformDatabase = null;
			}
		}

		/// <summary>
		/// Mangles the supplied GUID in a manner consistent with the instance.
		/// </summary>
		/// <param name="guid">The GUID.</param>
		/// <returns></returns>
		/// <remarks>
		/// By default this method returns the supplied guid.
		/// Override in derived classes to modify the supplied guid in some way.
		/// </remarks>
		public virtual Guid MangleGuid(Guid guid)
		{
			return guid;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Called when a transform is being created.
		/// </summary>
		/// <remarks>
		/// Derived classes should override this method to perform the
		/// operations required to create the transform.
		/// </remarks>
		protected virtual IEnumerator<TransformTask> OnCreateTransform()
		{
			return null;
		}

		/// <summary>
		/// Discards the temporary files associated with this transform 
		/// object.
		/// </summary>
		protected virtual void DiscardTemporaryFiles()
		{
			File.Delete(_transformDatabasePathName);
			File.Delete(TransformPathName);
		}

	    protected void LogMessage(string message)
	    {
	        if (Logger != null)
	        {
	            Logger.LogMessage(message);
	        }
	        else
	        {
	            Console.WriteLine(message);
	        }
	    }
		#endregion

		#region Private Methods
		#endregion

		#region IDisposable Members
		~MsiTransform()
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
				Close();

				if (!_keepFiles)
				{
					DiscardTemporaryFiles();
				}
			}
			InputDatabase = null;
			TransformDatabase = null;
		}
		#endregion
	}
}
