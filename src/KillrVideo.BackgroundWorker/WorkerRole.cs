using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.BackgroundWorker.Startup;
using KillrVideo.SampleData.Worker.Scheduler;
using KillrVideo.Uploads.Worker;
using log4net;
using log4net.Config;
using Microsoft.WindowsAzure.ServiceRuntime;
using Nimbus;

namespace KillrVideo.BackgroundWorker
{
    /// <summary>
    /// The main Azure entry point for the KillVideo.BackgroundWorker role.
    /// </summary>
    public class WorkerRole : RoleEntryPoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WorkerRole));

        private readonly List<Task> _backgroundTasks;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private IWindsorContainer _windsorContainer;

        public WorkerRole()
        {
            _backgroundTasks = new List<Task>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Bootstrap Log4net logging
            XmlConfigurator.Configure();

            Logger.Info("KillrVideo.BackgroundWorker is starting");

            try
            {
                // Initialize the Windsor container
                _windsorContainer = WindsorBootstrapper.CreateContainer();

                // Start the message bus
                var bus = _windsorContainer.Resolve<Bus>();
                bus.Start();

                // Start the Upload monitoring job
                CancellationToken token = _cancellationTokenSource.Token;
                var job = _windsorContainer.Resolve<EncodingListenerJob>();
                _backgroundTasks.Add(Task.Run(() => job.Execute(token), token));

                // Start the sample data worker job scheduler
                var sampleDataScheduler = _windsorContainer.Resolve<SampleDataJobScheduler>();
                _backgroundTasks.Add(Task.Run(() => sampleDataScheduler.Run(token), token));
                
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
                // Cancel the background tasks, then wait for them to finish
                _cancellationTokenSource.Cancel();
                Task.WhenAll(_backgroundTasks).Wait();
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
