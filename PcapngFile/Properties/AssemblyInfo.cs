﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("PCAP-NG File Reader")]
[assembly: AssemblyDescription("Reads PCAP Next Generation files and generates CLR objects from its data. Implemented according to the draft specification at http://www.winpcap.org/ntar/draft/PCAP-DumpFileFormat.html.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Andrew Walsh")]
[assembly: AssemblyProduct("PcapngFile")]
[assembly: AssemblyCopyright("Copyright © 2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5689565f-8c74-46a5-b8ff-b52aa0d40ef0")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.2.0")]
[assembly: AssemblyFileVersion("1.0.2.0")]

[assembly: InternalsVisibleTo("PcapngFile.Tests")]