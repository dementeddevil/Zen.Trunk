using System;
using Zen.Tasks.Wix.InstanceService.InstallerDatabase;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public class MsiInstanceTransformPacker : MsiTransformPacker
	{
		#region Private Fields
		private readonly string _binaryKeyPrefix;
	    private bool _mangleUpgradeCode = true;
		private bool _mangleProductCode = true;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="MsiInstanceTransformPacker"/> class.
		/// </summary>
		/// <param name="binaryKeyPrefix">The binary key prefix.</param>
		/// <param name="instanceCount">The instance count.</param>
		public MsiInstanceTransformPacker(string binaryKeyPrefix, int instanceCount)
		{
			_binaryKeyPrefix = binaryKeyPrefix;
			InstanceCount = instanceCount;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the instance count.
		/// </summary>
		/// <value>The instance count.</value>
		public int InstanceCount { get; }
	    #endregion

		#region Public Methods
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Generates the transforms.
		/// </summary>
		protected override void GenerateTransforms()
		{
			for (var index = 0; index < InstanceCount; ++index)
			{
				// Create and open the transform
				var tfm = CreateAndOpenTransform();

				// Retrieve the upgrade code
				var upgradeCode = UpgradeCode;

				// If the upgrade code is empty then retrieve the current
				//	code from the input database
				if (upgradeCode == Guid.Empty)
				{
					upgradeCode = new Guid(tfm.InputDatabase.GetPropertyValue("UpgradeCode"));
				}

				// If we are mangling upgrade codes then do that now
				if (_mangleUpgradeCode)
				{
					upgradeCode = tfm.MangleGuid(upgradeCode);
				}

				// Determine the product code
				var productCode = Guid.Empty;
				if (_mangleProductCode)
				{
					productCode = new Guid(tfm.InputDatabase.GetPropertyValue("ProductCode"));
					productCode = tfm.MangleGuid(productCode);
				}
				else
				{
					productCode = Guid.NewGuid();
				}

				// Create transform differences
				tfm.CreateTransform(productCode, upgradeCode);

				// Generate transformation file
				tfm.GenerateTransform(TransformError.None,
					TransformValidation.Product |
					TransformValidation.NewEqualBaseVersion);

				// Add transform to list
				Transforms.Add(tfm);
			}
		}

		/// <summary>
		/// Overridden. Creates the transform.
		/// </summary>
		/// <returns></returns>
		protected override MsiTransform CreateTransform()
		{
			return new MsiInstanceTransform(Transforms.Count + 1, KeepFiles);
		}

		/// <summary>
		/// Packages the transform.
		/// </summary>
		/// <param name="transform">The transform.</param>
		/// <returns></returns>
		/// <remarks>
		/// Only transform objects derived from <see cref="MsiInstanceTransform"/>
		/// will be automatically packaged by this method.
		/// </remarks>
		protected override bool PackageTransform(MsiTransform transform)
		{
			var packaged = false;
			var instanceTransform = transform as MsiInstanceTransform;
			if (instanceTransform != null)
			{
				string key = $"{_binaryKeyPrefix}{instanceTransform.Instance}.mst";
				PackageStorageFile(key, instanceTransform.TransformPathName);
				packaged = true;
			}
			return packaged;
		}
		#endregion

		#region Private Methods
		#endregion
	}
}
