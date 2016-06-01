using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private static readonly ILogger Logger = Log.ForContext<Host>();

        private readonly IEnumerable<IHostTask> _tasks;
        private readonly IHostConfiguration _hostConfig;

        public Host(IEnumerable<IHostTask> tasks, IHostConfiguration hostConfig)
        {
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));
            if (hostConfig == null) throw new ArgumentNullException(nameof(hostConfig));
            _tasks = tasks;
            _hostConfig = hostConfig;
        }

        public void Start()
        {
            Logger.Information("Starting Host {ApplicationName}", _hostConfig.ApplicationName);

            foreach (IHostTask task in _tasks)
            {
                Logger.Information("Starting Task {TaskName} on {ApplicationName}", task.Name, _hostConfig.ApplicationName);
                task.Start();
            }

            Logger.Information("Started Host {ApplicationName}", _hostConfig.ApplicationName);
        }

        public void Stop()
        {
            Logger.Information("Stopping Host {ApplicationName}", _hostConfig.ApplicationName);

            try
            {
                var stopping = new List<Task>();
                foreach (var task in _tasks)
                    stopping.Add(StopTask(task));

                Task.WaitAll(stopping.ToArray());
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping Host {ApplicationName}", _hostConfig.ApplicationName);
            }

            Logger.Information("Stopped Host {ApplicationName}", _hostConfig.ApplicationName);
        }

        private async Task StopTask(IHostTask task)
        {
            Logger.Information("Stopping Task {TaskName} on {ApplicationName}", task.Name, _hostConfig.ApplicationName);
            await task.StopAsync().ConfigureAwait(false);
            Logger.Information("Stopped Task {TaskName} on {ApplicationName}", task.Name, _hostConfig.ApplicationName);
        }
    }
}
