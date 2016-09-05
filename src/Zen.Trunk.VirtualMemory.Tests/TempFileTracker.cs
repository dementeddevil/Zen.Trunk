﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zen.Trunk
{
    public class TempFileTracker : IDisposable
    {
        private readonly string _testFolder;
        private readonly List<string> _trackedFiles = new List<string>();

        public TempFileTracker([CallerMemberName] string methodName = null)
        {
            var assembly = Assembly.GetCallingAssembly();

            // Get path to executing assembly
            var basePath = Path.GetDirectoryName(assembly
                .CodeBase
                .Replace("file:///", string.Empty));

            // Get to the solution root folder
            basePath = new DirectoryInfo(basePath).Parent.Parent.Parent.Parent.Parent.FullName;

            // Setup test results and assembly folder
            basePath = Path.Combine(basePath, "TestResults");
            basePath = Path.Combine(basePath, assembly.GetName().Name.Replace(".", string.Empty));

            // Setup folder for caller test class
            _testFolder = Path.Combine(basePath, methodName ?? Guid.NewGuid().ToString("N"));

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

                try
                {
                    Directory.Delete(_testFolder, true);
                }
                catch
                {
                }
            }
        }
    }
}
