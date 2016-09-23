using System.Collections.Generic;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Tasks.Wix.InstanceService.Transforms.MsiInstanceTransform" />
    public abstract class ServiceInstanceTransform : MsiInstanceTransform
	{
		#region Protected Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceInstanceTransform"/> class.
		/// </summary>
		/// <param name="instance">The instance index.</param>
		/// <param name="keepFiles">if set to <c>true</c> then the resultant transform will be kept.</param>
		protected ServiceInstanceTransform(int instance, bool keepFiles)
			: base(instance, keepFiles)
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the name of the product.
		/// </summary>
		/// <value>
		/// The name of the product.
		/// </value>
		public abstract string ProductName
		{
			get;
		}

		/// <summary>
		/// Gets the display name of the service.
		/// </summary>
		/// <value>
		/// The display name of the service.
		/// </value>
		public abstract string ServiceDisplayName
		{
			get;
		}

		/// <summary>
		/// Gets the service description.
		/// </summary>
		/// <value>
		/// The service description.
		/// </value>
		public abstract string ServiceDescription
		{
			get;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Called when a transform is being created.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// Derived classes should override this method to perform the
		/// operations required to create the transform.
		/// </remarks>
		protected override IEnumerator<TransformTask> OnCreateTransform()
		{
			// Modify service name
			var serviceName = "[InstancePrefix]$[INSTANCENAME]";
			yield return new TransformTask(1, "Update service name", serviceName);
			TransformDatabase.UpdatePropertyTable("SERVICENAME", serviceName);

			// Modify product name
			yield return new TransformTask(2, "Update product name", ProductName);
			TransformDatabase.UpdatePropertyTable("ProductName", ProductName);

			// Modify service install table
			yield return new TransformTask(3, "Update service install", string.Empty);
			TransformDatabase.ExecuteNonQuery(
			    $"UPDATE `ServiceInstall` SET `ServiceInstall`.`DisplayName`='{ServiceDisplayName}' WHERE `ServiceInstall`.`ServiceInstall`='ServiceInstall'");
			TransformDatabase.ExecuteNonQuery(
			    $"UPDATE `ServiceInstall` SET `ServiceInstall`.`Description`='{ServiceDescription}' WHERE `ServiceInstall`.`ServiceInstall`='ServiceInstall'");

			// Modify instance folder directory
			string instanceDir = $"{InstancePrefix}.{Instance}";
			yield return new TransformTask(4, "Update instance install folder", instanceDir);
			TransformDatabase.UpdateDirectoryTable("INSTANCEDIR", instanceDir);

			// Modify instance folder property
			yield return new TransformTask(5, "Update instance install property", instanceDir);
			TransformDatabase.UpdatePropertyTable("InstanceFolder", instanceDir);

			// Add instance index property
			yield return new TransformTask(6, "Update instance index property", Instance.ToString());
			TransformDatabase.UpdatePropertyTable("InstanceIndex", Instance.ToString());

			// Add instance index property
			yield return new TransformTask(7, "Update instance index property", Instance.ToString());
			TransformDatabase.UpdatePropertyTable("DefaultInstance", "0");
		}
		#endregion
	}
}
