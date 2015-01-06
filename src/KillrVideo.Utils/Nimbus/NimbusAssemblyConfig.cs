using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KillrVideo.Utils.Nimbus
{
    /// <summary>
    /// A static class containing configuration needed for Nimbus in the current app.
    /// </summary>
    public static class NimbusAssemblyConfig
    {
        /// <summary>
        /// A list of assemblies to scan for messages, handlers, etc.
        /// </summary>
        public static List<Assembly> AssembliesToScan { get; set; }

        static NimbusAssemblyConfig()
        {
            AssembliesToScan = new List<Assembly>();
        }

        /// <summary>
        /// Adds assemblies to the list of assemblies to scan based on the assemblies of the Types provided.
        /// </summary>
        public static void AddFromTypes(params Type[] types)
        {
            AssembliesToScan.AddRange(types.Select(t => t.Assembly));
        }
    }
}
