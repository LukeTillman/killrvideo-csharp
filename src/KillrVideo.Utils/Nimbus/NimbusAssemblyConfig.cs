using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KillrVideo.Utils.Nimbus
{
    /// <summary>
    /// A class containing configuration needed for Nimbus.
    /// </summary>
    public class NimbusAssemblyConfig
    {
        /// <summary>
        /// A list of assemblies to scan for messages, handlers, etc.
        /// </summary>
        public List<Assembly> AssembliesToScan { get; set; }

        public NimbusAssemblyConfig(IEnumerable<Assembly> assembliesToScan = null)
        {
            AssembliesToScan = assembliesToScan == null ? new List<Assembly>() : assembliesToScan.ToList();
        }

        /// <summary>
        /// Gets assembly config from a collection of types that are in the assemblies to scan.
        /// </summary>
        public static NimbusAssemblyConfig FromTypes(params Type[] types)
        {
            return new NimbusAssemblyConfig(types.Select(t => t.Assembly));
        }
    }
}
