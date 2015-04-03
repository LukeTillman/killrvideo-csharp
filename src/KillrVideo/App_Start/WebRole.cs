using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace KillrVideo
{
    /// <summary>
    /// Azure entry point for the Web Role (i.e. KillrVideo web site).
    /// </summary>
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // Turn down the verbosity of traces written by Azure
            RoleEnvironment.TraceSource.Switch.Level = SourceLevels.Information;
            return base.OnStart();
        }
    }
}