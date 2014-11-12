using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.UploadWorker.Jobs;
using KillrVideo.UploadWorker.Startup;
using log4net;
using log4net.Config;
using Microsoft.WindowsAzure.ServiceRuntime;
using Rebus;

namespace KillrVideo.UploadWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WorkerRole));

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Task> _tasks;

        private IWindsorContainer _windsorContainer;
        
        public WorkerRole()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _tasks = new List<Task>();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Bootstrap Log4net logging
            XmlConfigurator.Configure();

            Logger.Info("KillrVideo.UploadWorker is starting");

            try
            {
                // Initialize the Windsor container
                _windsorContainer = WindsorBootstrapper.CreateContainer();

                // Start the message bus
                var bus = _windsorContainer.Resolve<IStartableBus>();
                bus.Start();

                // Use the container to get any jobs to run and execute them as Tasks
                CancellationToken token = _cancellationTokenSource.Token;
                IUploadWorkerJob[] jobs = _windsorContainer.ResolveAll<IUploadWorkerJob>();
                foreach (IUploadWorkerJob job in jobs)
                {
                    IUploadWorkerJob currentJob = job;
                    _tasks.Add(Task.Run(() => currentJob.Execute(token), token));
                }
                
                return base.OnStart();
            }
            catch (Exception e)
            {
                Logger.Error("Exception in UploadWorker OnStart", e);
                throw;
            }
        }
        
        public override void OnStop()
        {
            Logger.Info("KillrVideo.UploadWorker is stopping");

            try
            {
                // Cancel any tasks in progress and wait for them to finish
                _cancellationTokenSource.Cancel();
                Task.WaitAll(_tasks.ToArray());
            }
            catch (AggregateException ae)
            {
                foreach (Exception exception in ae.InnerExceptions)
                {
                    // Ignore OperationCancelledExceptions since we expect those
                    var operationCancelled = exception as OperationCanceledException;
                    if (operationCancelled != null)
                        continue;

                    Logger.Error("Unexpected exception while cancelling Tasks in UploadWorker stop", exception);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Unexpected error during UploadWorker stop", e);
            }

            // Dispose of Windsor container
            if (_windsorContainer != null)
                _windsorContainer.Dispose();

            base.OnStop();
        }
    }
}
