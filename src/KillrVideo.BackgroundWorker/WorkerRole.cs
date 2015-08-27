using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.BackgroundWorker.Startup;
using KillrVideo.Utils.WorkerComposition;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

namespace KillrVideo.BackgroundWorker
{
    /// <summary>
    /// The main Azure entry point for the KillVideo.BackgroundWorker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        private IWindsorContainer _windsorContainer;
        private ILogicalWorkerRole[] _logicalWorkers;

        public override bool OnStart()
        {
            // Turn down the verbosity of traces written by Azure
            RoleEnvironment.TraceSource.Switch.Level = SourceLevels.Information;

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Bootstrap Serilog logger
            Log.Logger = new LoggerConfiguration().WriteTo.Trace().CreateLogger();

            Log.Information("KillrVideo.BackgroundWorker is starting");

            try
            {
                // Initialize the Windsor container
                _windsorContainer = WindsorBootstrapper.CreateContainer();

                // Create the logical worker instances and fire async OnStart
                _logicalWorkers = new ILogicalWorkerRole[]
                {
                    new VideoCatalog.Worker.WorkerRole(_windsorContainer),
                    new Search.Worker.WorkerRole(_windsorContainer),
                    new Uploads.Worker.WorkerRole(_windsorContainer),
                    new SampleData.Worker.WorkerRole(_windsorContainer)
                };

                Task[] startTasks = _logicalWorkers.Select(w => Task.Run(() => w.OnStart())).ToArray();

                // Wait for all workers to start
                Task.WaitAll(startTasks);

                return base.OnStart();
            }
            catch (AggregateException ae)
            {
                foreach (var exception in ae.Flatten().InnerExceptions)
                    Log.Fatal(exception, "Unexpected exception while starting background worker");

                throw new Exception("Background worker failed to start", ae);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unexpected exception while starting background worker");
                throw;
            }
        }

        public override void OnStop()
        {
            Log.Information("KillrVideo.BackgroundWorker is stopping");

            // Stop all logical workers
            if (_logicalWorkers != null)
            {
                try
                {
                    Task[] stopTasks = _logicalWorkers.Select(w => w.OnStop()).ToArray();
                    Task.WaitAll(stopTasks);
                }
                catch (AggregateException ae)
                {
                    // Log any exceptions
                    foreach (var exception in ae.Flatten().InnerExceptions)
                        Log.Error(exception, "Unexpected exception while cancelling Tasks in BackgroundWorker stop");
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected error during BackgroundWorker stop");
                }
            }

            // Dispose of Windsor container
            if (_windsorContainer != null)
                _windsorContainer.Dispose();

            base.OnStop();
        }
    }
}
