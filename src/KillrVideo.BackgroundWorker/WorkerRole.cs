using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Castle.Windsor;
using KillrVideo.BackgroundWorker.Startup;
using KillrVideo.Uploads.Worker;
using log4net;
using log4net.Config;
using Microsoft.WindowsAzure.ServiceRuntime;
using Nimbus;

namespace KillrVideo.BackgroundWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (WorkerRole));

        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _uploadMonitor;

        private IWindsorContainer _windsorContainer;
        
        public WorkerRole()
        {
            _cancellationTokenSource = new CancellationTokenSource();
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
                var bus = _windsorContainer.Resolve<Bus>();
                bus.Start();

                // Start the Upload monitoring job
                CancellationToken token = _cancellationTokenSource.Token;
                var job = _windsorContainer.Resolve<EncodingListenerJob>();
                _uploadMonitor = Task.Run(() => job.Execute(token), token);

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
                // Cancel the upload monitor task and wait for it to finish
                _cancellationTokenSource.Cancel();
                if (_uploadMonitor != null)
                    _uploadMonitor.Wait();
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
