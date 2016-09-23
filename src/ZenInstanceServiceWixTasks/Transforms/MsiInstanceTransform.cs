using System;
using System.Collections.Generic;
using Zen.Tasks.Wix.InstanceService.InstallerDatabase;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public class MsiInstanceTransform : MsiTransform
	{
		#region Private Fields
	    #endregion

		#region Public Constructors
	    /// <summary>
		/// Initializes a new instance of the <see cref="MsiInstanceTransform"/> class.
		/// </summary>
		/// <param name="instance">The instance index.</param>
		/// <param name="keepFiles">
		/// if set to <c>true</c> then the resultant transform will be kept.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if the instance index is less than one or greater than 64.
		/// </exception>
		public MsiInstanceTransform(int instance, bool keepFiles = true)
			: base(keepFiles)
		{
			if (instance < 1 || instance > 64)
			{
				throw new ArgumentOutOfRangeException(nameof(instance), instance,
					"Instance index out of range (1 thru 64).");
			}
			Instance = instance;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the instance prefix.
		/// </summary>
		/// <value>The instance prefix.</value>
		public string InstancePrefix { get; private set; }

	    /// <summary>
		/// Gets the instance.
		/// </summary>
		/// <value>The instance.</value>
		public int Instance { get; }
	    #endregion

		#region Public Methods
		/// <summary>
		/// Overridden. Opens the database.
		/// </summary>
		/// <param name="inputDatabasePathName">Name of the input database path.</param>
		/// <param name="workingFolder">The working folder.</param>
		/// <param name="transformBaseName">Name of the transform base.</param>
		public override void Open(string inputDatabasePathName,
			string workingFolder, string transformBaseName)
		{
			base.Open(inputDatabasePathName, workingFolder, $"{transformBaseName}{Instance}");

			InstancePrefix = InputDatabase.GetPropertyValue("InstancePrefix");
		}

		/// <summary>
		/// Gets the collection of component GUIDs from the installer database.
		/// </summary>
		/// <returns></returns>
		public Guid[] GetComponentGuids()
		{
			// Build list of component GUIDs
			var componentGuidList = new List<Guid>();
			using (var view = TransformDatabase.OpenView(
				"SELECT `Component`.`ComponentId` FROM `Component`"))
			{
				// Execute the query
				view.Execute(null);

				var record = new MsiRecord(1);
				while (view.Fetch(record))
				{
					var oldGuid = record[1].GetString();
					componentGuidList.Add(new Guid(oldGuid));
				}
			}
			return componentGuidList.ToArray();
		}

		/// <summary>
		/// Mangles the supplied GUID in a manner consistent with the instance.
		/// </summary>
		/// <param name="guid">The GUID.</param>
		/// <returns></returns>
		/// <remarks>
		/// The supplied GUID will always be modified in a consistent manner
		/// for a given instance to ensure we can upgrade properly.
		/// </remarks>
		public override Guid MangleGuid(Guid guid)
		{
			// We will modify the supplied GUID in a manner that will always
			//	produce the same result for a given combination of GUID and
			//	instance index - this ensures upgrades will continue to work.
			var guidBuffer = guid.ToByteArray();

			// Adjust byte 2 and byte 5
			guidBuffer[2] += (byte)(Instance);
			guidBuffer[5] += (byte)(Instance * 2);
			guidBuffer[7] += (byte)(Instance * 3);
			return new Guid(guidBuffer);
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		#endregion
	}
}
