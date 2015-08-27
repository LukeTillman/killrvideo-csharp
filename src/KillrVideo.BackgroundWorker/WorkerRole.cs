using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Task> _logicalWorkerTasks;

        private IWindsorContainer _windsorContainer;

        public WorkerRole()
        {
            _logicalWorkerTasks = new List<Task>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

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

                // Create the logical worker instances
                var logicalWorkers = new ILogicalWorkerRole[]
                {
                    new VideoCatalog.Worker.WorkerRole(_windsorContainer),
                    new Search.Worker.WorkerRole(_windsorContainer),
                    new Uploads.Worker.WorkerRole(_windsorContainer),
                    new SampleData.Worker.WorkerRole(_windsorContainer)
                };

                // Fire OnStart on all the logical workers
                CancellationToken token = _cancellationTokenSource.Token;
                foreach (ILogicalWorkerRole worker in logicalWorkers)
                {
                    ILogicalWorkerRole worker1 = worker;
                    _logicalWorkerTasks.Add(Task.Run(() => worker1.OnStart(token), token));
                }
                
                return base.OnStart();
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception in BackgroundWorker OnStart");
                throw;
            }
        }
        
        public override void OnStop()
        {
            Log.Information("KillrVideo.BackgroundWorker is stopping");

            try
            {
                // Cancel the logical worker tasks, then wait for them to finish
                _cancellationTokenSource.Cancel();
                Task.WhenAll(_logicalWorkerTasks).Wait();
            }
            catch (AggregateException ae)
            {
                // Log any exceptions that aren't OperationCanceled, which we expect
                foreach(var exception in ae.Flatten().InnerExceptions.Where(e => e is OperationCanceledException == false))
                    Log.Error(exception, "Unexpected exception while cancelling Tasks in BackgroundWorker stop");
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error during BackgroundWorker stop");
            }

            // Dispose of Windsor container
            if (_windsorContainer != null)
                _windsorContainer.Dispose();

            base.OnStop();
        }
    }
}
