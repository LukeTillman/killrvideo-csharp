using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KillrVideo.Configuration;
using KillrVideo.Host;
using KillrVideo.Host.Config;
using Serilog;
using System.Collections.Generic;
using Cassandra;

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
        private readonly Lazy<IDictionary<string, string>> _dockerToolboxVariables;

        public BootstrapDocker(IHostConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _config = config;
            _dockerToolboxVariables = new Lazy<IDictionary<string, string>>(LoadDockerToolboxVariables);
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

        private Process CreateDockerComposeProcess(string arguments)
        {
            Process p = CreateProcess("docker-compose", arguments);

            // Find out if we're using Docker Toolbox and if so, load environment variables into the process
            bool isDockerToolbox = bool.Parse(_config.GetRequiredConfigurationValue(ConfigConstants.DockerToolbox));
            if (isDockerToolbox)
            {
                foreach (KeyValuePair<string, string> kvp in _dockerToolboxVariables.Value)
                    p.StartInfo.Environment.Add(kvp);
            }

            return p;
        }

        private static Process CreateProcess(string filename, string arguments)
        {
            return new Process
            {
                StartInfo =
                {
                    FileName = filename,
                    Arguments = arguments,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
        }

        private static IDictionary<string, string> LoadDockerToolboxVariables()
        {
            Logger.Information("Loading Docker Toolbox environment from docker machine");

            var results = new Dictionary<string, string>();

            // Run the docker-machine env command which will produce output that code be used to configure a cmd shell
            using (Process dockerMachine = CreateProcess("docker-machine", "env --shell cmd"))
            {
                // Just write errors to the console
                dockerMachine.ErrorDataReceived += (o, args) => Console.Error.WriteLine(args.Data);

                // Parse the output looking for environment variables being set
                dockerMachine.OutputDataReceived += (o, args) =>
                {
                    bool isSet = args.Data?.StartsWith("SET ") ?? false;
                    if (isSet == false)
                        return;

                    string envLine = args.Data.Substring(4);
                    string[] envParts = envLine.Split('=');
                    results.Add(envParts[0], envParts[1]);
                };

                // Start docker-machine and begin reading error/output events
                dockerMachine.Start();
                dockerMachine.BeginErrorReadLine();
                dockerMachine.BeginOutputReadLine();
                
                dockerMachine.WaitForExit();

                if (dockerMachine.ExitCode != 0)
                    throw new InvalidOperationException($"Docker machine exited with code {dockerMachine.ExitCode}");
            }

            Logger.Information("Loaded Docker Toolbox environment");
            return results;
        }
    }
}
