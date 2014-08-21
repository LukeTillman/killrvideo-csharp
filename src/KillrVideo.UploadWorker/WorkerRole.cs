using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.UploadWorker.Jobs;
using KillrVideo.UploadWorker.Startup;
using log4net;
using log4net.Config;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace KillrVideo.UploadWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WorkerRole));

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<Task> _tasks;

        private WindsorContainer _windsorContainer;
        
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

            try
            {
                // Initialize the Windsor container
                _windsorContainer = WindsorBootstrapper.CreateContainer();

                CancellationToken token = _cancellationTokenSource.Token;

                // Use the container to get any jobs to run and execute them as Tasks
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
