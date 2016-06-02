using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        public void Start()
        {
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
