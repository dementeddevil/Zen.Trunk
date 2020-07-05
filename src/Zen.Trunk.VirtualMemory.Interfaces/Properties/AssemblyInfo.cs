using System;

#if !NETCOREAPP5_0
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Zen Trunk VirtualMemory")]
[assembly: AssemblyDescription("Trunk virtual memory management framework")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Zen Design Software")]
[assembly: AssemblyProduct("Zen Trunk")]
[assembly: AssemblyCopyright("Copyright © Zen Design Software 2010 - 2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("560e1726-6ab6-4fb6-bf0e-d0518d2b300f")]

// Version information
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("Zen.Trunk.VirtualMemory.Tests")]
#endif

[assembly: CLSCompliant(false)]
