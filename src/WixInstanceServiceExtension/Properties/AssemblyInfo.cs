using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Tools.WindowsInstallerXml;
using Zen.WindowsInstallerXml.Extensions;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("WiX Toolset InstanceService Extension")]
[assembly: AssemblyDescription("Windows Installer XML Toolset Service UI Extension")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: AssemblyDefaultWixExtension(typeof(InstanceServiceExtension))]
