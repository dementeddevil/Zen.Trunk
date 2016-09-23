using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Zen.Tasks.Wix.InstanceService.Transforms;

namespace Zen.Tasks.Wix.InstanceService
{
    public class GenerateServiceInstallerTransformsTask : Task
    {
        public int InstanceCount { get; set; } = 16;

        [Required]
        public string BaseName { get; set; }

        [Required]
        public string Description { get; set; }

        public bool IncludeVersionInProductName { get; set; } = true;

        public bool KeepFiles { get; set; } = true;

        [Required]
        public ITaskItem[] InputDatabases { get; set; }

        [Output]
        public ITaskItem[] OutputDatabases { get; set; }

        public Guid UpgradeCode { get; set; }

        [Required]
        public string WorkingFolder { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            var outputDatabases = new List<ITaskItem>();
            foreach (var inputDatabase in InputDatabases)
            {
                // Build input and output pathnames
                var inputPathName = inputDatabase.ItemSpec;
                if (!string.IsNullOrWhiteSpace(inputPathName))
                {
                    var outputFileName = Path.GetFileNameWithoutExtension(inputDatabase.ItemSpec) + "_new.msi";
                    var outputPathName = Path.Combine(Path.GetDirectoryName(inputPathName), outputFileName);
                    Log.LogMessage($"Input: {inputPathName}, Output: {outputPathName}");
                    var transformPacker =
                        new SimpleServiceInstanceTransformPacker(
                            InstanceCount,
                            BaseName,
                            Description,
                            IncludeVersionInProductName)
                        { Logger = Log };
                    transformPacker.CreateTransformDatabase(
                        inputPathName,
                        outputPathName,
                        WorkingFolder,
                        UpgradeCode,
                        KeepFiles);

                    outputDatabases.Add(new TaskItem(outputPathName));
                }
            }

            OutputDatabases = outputDatabases.ToArray();
            return true;
        }
    }
}
