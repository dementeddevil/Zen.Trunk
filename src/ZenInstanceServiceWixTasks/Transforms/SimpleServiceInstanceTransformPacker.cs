namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Tasks.Wix.InstanceService.Transforms.MsiInstanceTransformPacker" />
    public class SimpleServiceInstanceTransformPacker : MsiInstanceTransformPacker
    {
        private readonly string _baseName;
        private readonly string _description;
        private readonly bool _includeVersionInProductName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleServiceInstanceTransformPacker"/> class.
        /// </summary>
        /// <param name="instanceCount">The instance count.</param>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="description">The description.</param>
        /// <param name="includeVersionInProductName">if set to <c>true</c> [include version in product name].</param>
        public SimpleServiceInstanceTransformPacker(
            int instanceCount,
            string baseName, 
            string description, 
            bool includeVersionInProductName)
            : base("InstanceTransform", instanceCount)
        {
            _baseName = baseName;
            _description = description;
            _includeVersionInProductName = includeVersionInProductName;
        }

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
            return new SimpleServiceInstanceTransform(
                Transforms.Count + 1, KeepFiles,
                _baseName, _description, _includeVersionInProductName)
            {
                Logger = Logger
            };
        }
    }
}