using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using KillrVideo.Host.Config;
using KillrVideo.Host.Tasks;
using Serilog;

namespace KillrVideo.Host
{
    /// <summary>
    /// A KillrVideo host that can be started/stopped.
    /// </summary>
    [Export]
    public class Host
    {
        private static readonly ILogger Logger = Logger.ForContext<Host>();

        private readonly IEnumerable<IHostTask> _tasks;
        private string _name;

        public Host(IEnumerable<IHostTask> tasks)
        {
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));
            _tasks = tasks;
        }

        public void Start(string name, IHostConfiguration hostConfig)
        {
            _name = name;

            Logger.Information("Starting KillrVideo Host {HostName}", name);

            foreach (IHostTask task in _tasks)
            {
                Logger.Information("Starting Task {TaskName} on {HostName}", task.Name, name);
                task.Start(hostConfig);
            }
        }

        public void Stop()
        {
            try
            {
                Task.WaitAll(_tasks.Select(t => t.StopAsync()).ToArray());
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping host {HostName}", _name);
            }
        }
    }
}
