using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.BackgroundWorker.Startup;
using KillrVideo.Utils.WorkerComposition;
using log4net;
using log4net.Config;
using Microsoft.WindowsAzure.ServiceRuntime;
using Serilog;

namespace KillrVideo.BackgroundWorker
{
    /// <summary>
    /// The main Azure entry point for the KillVideo.BackgroundWorker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WorkerRole));

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
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Bootstrap Log4net logging and serilog logger
            XmlConfigurator.Configure();
            Log.Logger = new LoggerConfiguration().WriteTo.Log4Net().CreateLogger();

            Logger.Info("KillrVideo.BackgroundWorker is starting");

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
                Logger.Error("Exception in BackgroundWorker OnStart", e);
                throw;
            }
        }
        
        public override void OnStop()
        {
            Logger.Info("KillrVideo.BackgroundWorker is stopping");

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
                    Logger.Error("Unexpected exception while cancelling Tasks in BackgroundWorker stop", exception);
            }
            catch (Exception e)
            {
                Logger.Error("Unexpected error during BackgroundWorker stop", e);
            }

            // Dispose of Windsor container
            if (_windsorContainer != null)
                _windsorContainer.Dispose();

            base.OnStop();
        }
    }
}
