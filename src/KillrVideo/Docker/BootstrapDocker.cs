using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KillrVideo.Configuration;
using KillrVideo.Host;
using KillrVideo.Host.Config;
using Serilog;

namespace KillrVideo.Docker
{
    /// <summary>
    /// Class for starting docker dependencies.
    /// </summary>
    [Export(typeof(BootstrapDocker))]
    public class BootstrapDocker
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(BootstrapDocker));
        private readonly IHostConfiguration _config;

        public BootstrapDocker(IHostConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config;
        }

        public void Start()
        {
            // See if we need to start docker-compose
            string shouldStart = _config.GetConfigurationValueOrDefault(ConfigConstants.StartDockerCompose,
                bool.TrueString);

            if (bool.Parse(shouldStart) == false)
            {
                Logger.Debug("Starting Docker is disabled, skipping");
                return;
            }

            Logger.Information("Starting Docker dependencies via docker-compose");

            using (Process process = CreateDockerComposeProcess("up -d"))
            using (Stream outStream = Console.OpenStandardOutput())
            using (Stream errorStream = Console.OpenStandardError())
            {
                var copyTasks = new Task[2];
                process.Start();

                copyTasks[0] = process.StandardOutput.BaseStream.CopyToAsync(outStream);
                copyTasks[1] = process.StandardError.BaseStream.CopyToAsync(errorStream);

                process.WaitForExit();
                Task.WaitAll(copyTasks);

                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Docker compose start failed.");
            }

            Logger.Information("Started Docker dependencies");
        }

        public void Stop()
        {
            // See if we should stop docker dependencies
            string shouldStop = _config.GetConfigurationValueOrDefault(ConfigConstants.StopDockerCompose,
                bool.TrueString);
            if (bool.Parse(shouldStop) == false)
            {
                Logger.Debug("Stopping Docker is disabled, skipping");
                return;
            }

            Logger.Information("Stopping Docker dependencies via docker-compose");

            using (Process process = CreateDockerComposeProcess("stop"))
            using (Stream outStream = Console.OpenStandardOutput())
            using (Stream errorStream = Console.OpenStandardError())
            {
                var copyTasks = new Task[2];
                process.Start();

                copyTasks[0] = process.StandardOutput.BaseStream.CopyToAsync(outStream);
                copyTasks[1] = process.StandardError.BaseStream.CopyToAsync(errorStream);

                process.WaitForExit();
                Task.WaitAll(copyTasks);
            }
            
            Logger.Information("Stopped Docker dependencies");
        }

        private static Process CreateDockerComposeProcess(string arguments)
        {
            return new Process
            {
                StartInfo =
                {
                    FileName = "docker-compose",
                    Arguments = arguments,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
        }
    }
}
