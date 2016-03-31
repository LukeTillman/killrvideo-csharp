using System;

namespace KillrVideo.Cassandra
{
    /// <summary>
    /// Enum that represents the current state of any DataStax Enterprise install that's being used in the environment.
    /// </summary>
    [Flags]
    public enum DataStaxEnterpriseEnvironmentState
    {
        None = 0,
        SearchEnabled = 1,
        SparkEnabled = 2
    }
}
