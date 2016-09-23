namespace Zen.Tasks.Wix.InstanceService.Transforms
{
    public class SimpleServiceInstanceTransform : ServiceInstanceTransform
    {
        public SimpleServiceInstanceTransform(
            int instance, bool keepFiles,
            string baseName, string description, bool includeVersionInProductName)
            : base(instance, keepFiles)
        {
            ProductName = 
                $"{baseName} ([INSTANCENAME])" +
                (includeVersionInProductName ? " v[ProductVersion]" : string.Empty);
            ServiceDisplayName = $"{baseName} ([INSTANCENAME])";
            ServiceDescription = description;
        }

        public override string ProductName { get; }

        public override string ServiceDisplayName { get; }

        public override string ServiceDescription { get; }
    }
}