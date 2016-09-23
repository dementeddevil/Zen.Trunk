using System;

namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public class DefaultInstanceTransformPacker : MsiInstanceTransformPacker
	{
		#region Private Fields
		private readonly Type _transformType;
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultInstanceTransformPacker"/> class.
		/// </summary>
		/// <param name="instanceCount">The instance count.</param>
		/// <param name="transformType">Type of the transform.</param>
		public DefaultInstanceTransformPacker(int instanceCount, Type transformType)
			: base("InstanceTransform", instanceCount)
		{
			if (!typeof(MsiTransform).IsAssignableFrom(transformType))
			{
				throw new ArgumentException(
					"Transform type does not derive from MsiTransform.",
					nameof(transformType));
			}

			_transformType = transformType;
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Overridden. Creates the output database.
		/// </summary>
		/// <remarks>
		/// <para>
		/// By default the output database is a straight copy of the input
		/// database and is opened in transact mode.
		/// An installer property MaxInstance is added to the property table
		/// so that this can be validated during setup.
		/// </para>
		/// <para>
		/// The <see cref="P:Database"/> property is valid after this call.
		/// </para>
		/// </remarks>
		protected override void CreateOutputDatabase()
		{
			// Do base output database creation.
			base.CreateOutputDatabase();

			// Write out maximum instance property
			OutputDatabase.InsertPropertyTable("MaxInstance", InstanceCount.ToString());

			// TODO: Validate the instancedir directory - it should be the following;
			//	[InstancePrefix].[InstanceIndex] however the field is not formatted.
		}

		/// <summary>
		/// Overridden. Creates the transform.
		/// </summary>
		/// <returns></returns>
		protected override MsiTransform CreateTransform()
		{
			return (MsiTransform)Activator.CreateInstance(_transformType, Transforms.Count + 1, KeepFiles);
		}
		#endregion
	}

	public class DefaultInstanceTransformPacker<T> : DefaultInstanceTransformPacker
		where T : MsiTransform
	{
		#region Public Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultInstanceTransformPacker{T}"/> class.
		/// </summary>
		/// <param name="instanceCount">The instance count.</param>
		public DefaultInstanceTransformPacker(int instanceCount)
			: base(instanceCount, typeof(T))
		{
		}
		#endregion
	}
}
