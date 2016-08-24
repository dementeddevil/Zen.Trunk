using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.StorageEngine.Tests
{
    public class TempFileTracker : IDisposable
    {
        private readonly string _basePath;
        private readonly string _testFolder;
        private readonly List<string> _trackedFiles = new List<string>();

        public TempFileTracker([CallerMemberName] string methodName = null)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Get path to executing assembly
            _basePath = Path.GetDirectoryName(assembly
                .CodeBase
                .Replace("file:///", string.Empty));

            // Get to the solution root folder
            _basePath = new DirectoryInfo(_basePath).Parent.Parent.Parent.Parent.Parent.FullName;

            // Setup test results and assembly folder
            _basePath = Path.Combine(_basePath, "TestResults");
            _basePath = Path.Combine(_basePath, assembly.GetName().Name.Replace(".", string.Empty));

            // Setup folder for caller test class
            _testFolder = Path.Combine(_basePath, methodName ?? Guid.NewGuid().ToString("N"));

            // Create if it doesn't exist
            if (Directory.Exists(_testFolder))
            {
                Directory.Delete(_testFolder, true);
            }
        }

        public string Get(string filename)
        {
            if (!Directory.Exists(_testFolder))
            {
                Directory.CreateDirectory(_testFolder);
            }

            var trackedPathname = Path.Combine(_testFolder, filename);
            if (!_trackedFiles.Contains(trackedPathname))
            {
                _trackedFiles.Add(trackedPathname);
            }
            return trackedPathname;
        }

        public void Dispose()
        {
            if (Directory.Exists(_testFolder))
            {
                foreach (var file in _trackedFiles.Where(File.Exists))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }

                Directory.Delete(_testFolder, true);
            }
        }
    }
}
