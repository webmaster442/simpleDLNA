using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: Guid("381b29a0-7e27-49fa-ac74-f740c2da1f00")]
#endif
