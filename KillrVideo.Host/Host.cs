using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
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
        private readonly HostOptions _options;

        public Host(IEnumerable<IHostTask> tasks, HostOptions options)
        {
            if (tasks == null) throw new ArgumentNullException(nameof(tasks));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _tasks = tasks;
            _options = options;
        }

        public void Start()
        {
            Logger.Information("Starting Host {AppName}:{AppInstance}", _options.AppName, _options.AppInstance);

            foreach (IHostTask task in _tasks)
            {
                Logger.Information("Starting Task {TaskName} on {AppName}:{AppInstance}", task.Name, _options.AppName, _options.AppInstance);
                task.Start();
            }

            Logger.Information("Started Host {AppName}:{AppInstance}", _options.AppName, _options.AppInstance);
        }

        public void Stop()
        {
            Logger.Information("Stopping Host {AppName}:{AppInstance}", _options.AppName, _options.AppInstance);

            try
            {
                var stopping = new List<Task>();
                foreach (var task in _tasks)
                    stopping.Add(StopTask(task));

                Task.WaitAll(stopping.ToArray());
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while stopping Host {AppName}:{AppInstance}", _options.AppName, _options.AppInstance);
            }

            Logger.Information("Stopped Host {AppName}:{AppInstance}", _options.AppName, _options.AppInstance);
        }

        private async Task StopTask(IHostTask task)
        {
            Logger.Information("Stopping Task {TaskName} on {AppName}:{AppInstance}", task.Name, _options.AppName, _options.AppInstance);
            await task.StopAsync().ConfigureAwait(false);
            Logger.Information("Stopped Task {TaskName} on {AppName}:{AppInstance}", task.Name, _options.AppName, _options.AppInstance);
        }
    }
}
