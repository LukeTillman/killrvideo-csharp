using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace KillrVideo.Uploads.Worker.Jobs
{
    /// <summary>
    /// A base class for UploadWorker jobs that runs code in a loop with a configurable (in the constructor) delay
    /// between iterations of the loop.
    /// </summary>
    public abstract class UploadWorkerJobBase : IUploadWorkerJob
    {
        private readonly TimeSpan _delayBetweenIterations;
        private readonly ILog _logger;

        protected UploadWorkerJobBase()
            : this(TimeSpan.FromSeconds(10))
        {
        }

        protected UploadWorkerJobBase(TimeSpan delayBetweenIterations)
        {
            _delayBetweenIterations = delayBetweenIterations;
            _logger = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Executes the job.
        /// </summary>
        public async Task Execute(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ExecuteImpl(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    _logger.Error("Error while processing job", e);
                }

                await Task.Delay(_delayBetweenIterations, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The main method for the job which will be executed inside a loop body, with any delay specified in the constructor
        /// between iterations.  Any Exceptions thrown (other than OperationCanceledException) will be logged before continuing
        /// to the next iteration after the configured delay.
        /// </summary>
        protected abstract Task ExecuteImpl(CancellationToken cancellationToken);
    }
}
