using System;
using System.Collections.Generic;
using System.Reflection;

namespace KillrVideo.Utils
{
    /// <summary>
    /// Some utility extension methods.
    /// </summary>
    public static class HelperExtensions
    {
        private const long TicksPerMicrosecond = 10;

        /// <summary>
        /// Converts an IEnumerable&lt;T&gt; to a HashSet&lt;T&gt;.  If the IEnumerable is null, returns an empty HashSet.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null) return new HashSet<T>();
            return new HashSet<T>(enumerable);
        }

        /// <summary>
        /// Truncates a DateTimeOffset to the specified resolution.  Use the TimeSpan.TicksPerXXX constants for
        /// the resolution parameter.  Returns a new DateTimeOffset.
        /// </summary>
        public static DateTimeOffset Truncate(this DateTimeOffset dateTimeOffset, long resolution)
        {
            return new DateTimeOffset(dateTimeOffset.Ticks - (dateTimeOffset.Ticks % resolution), dateTimeOffset.Offset);
        }

        /// <summary>
        /// Adds the number of microseconds specified to the DateTimeOffset and returns a new DateTimeOffset.
        /// </summary>
        public static DateTimeOffset AddMicroseconds(this DateTimeOffset dateTimeOffset, long microSeconds)
        {
            return dateTimeOffset.AddTicks(microSeconds*TicksPerMicrosecond);
        }

        /// <summary>
        /// Recursively gets all application assemblies (i.e. that start with the same name as this assembly) that are referenced.
        /// </summary>
        public static HashSet<Assembly> GetReferencedApplicationAssemblies(this Assembly appAssembly)
        {
            // Figure out the application name from the assembly provided
            string name = appAssembly.GetName().Name;
            int idx = name.IndexOf('.');
            string applicationName = idx > 0 ? name.Substring(0, idx) : name;
            
            var assemblies = new HashSet<Assembly>();

            // Recursively add all application assemblies to the HashSet and return them
            AddApplicationAssemblies(appAssembly, assemblies, applicationName);
            return assemblies;
        }

        private static void AddApplicationAssemblies(Assembly appAssembly, HashSet<Assembly> assemblySet, string applicationName)
        {
            // Try to add the assembly provided and if we've already added this assembly, we don't need to process it again
            if (assemblySet.Add(appAssembly) == false)
                return;

            // Process all referenced assemblies since we haven't done this yet
            foreach (AssemblyName assemblyName in appAssembly.GetReferencedAssemblies())
            {
                // Skip any assemblies that are not in our application
                if (assemblyName.FullName.StartsWith(applicationName) == false)
                    continue;

                // Add the referenced assembly and its references
                Assembly referencedAssembly = Assembly.Load(assemblyName);
                AddApplicationAssemblies(referencedAssembly, assemblySet, applicationName);
            }
        }
    }
}
