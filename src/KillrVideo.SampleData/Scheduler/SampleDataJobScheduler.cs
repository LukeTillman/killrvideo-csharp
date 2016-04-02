using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KillrVideo.Host;
using KillrVideo.Host.Tasks;
using Serilog;

namespace KillrVideo.SampleData.Scheduler
{
    /// <summary>
    /// A scheduler that will run sample data jobs on a schedule.
    /// </summary>
    [Export(typeof(IHostTask))]
    public class SampleDataJobScheduler : IHostTask
    {
        private static readonly ILogger Logger = Log.ForContext<SampleDataJobScheduler>();
        private static readonly TimeSpan RetryOnExceptionWaitTime = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TimeBeforeExpirationToRenew = TimeSpan.FromSeconds(20);

        private readonly LeaseManager _leaseManager;
        private readonly List<SampleDataJob> _jobs;

        private readonly CancellationTokenSource _cancellation;
        private Task _runningServer;

        public string Name => "Sample Data Scheduler Server";

        public SampleDataJobScheduler(LeaseManager leaseManager, IEnumerable<SampleDataJob> jobs)
        {
            if (leaseManager == null) throw new ArgumentNullException(nameof(leaseManager));
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            _leaseManager = leaseManager;
            _jobs = jobs.ToList();

            _cancellation = new CancellationTokenSource();
            _runningServer = Task.CompletedTask;
        }

        public void Start()
        {
            Logger.Information("Starting sample data job scheduler with {JobsCount} jobs", _jobs.Count);
            _runningServer = Run(_cancellation.Token);
            Logger.Information("Started sample data job scheduler");
        }

        public async Task StopAsync()
        {
            Logger.Information("Stopping sample data job scheduler");

            try
            {
                _cancellation.Cancel();
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.IgnoreTaskCanceled())
                    Logger.Error(ex, "Unexpected exception while cancelling sample data scheduler");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Unexpected exception while cancelling sample data scheduler");
            }

            await _runningServer.ConfigureAwait(false);
            Logger.Information("Stopped sample data job scheduler");
        }

        /// <summary>
        /// Starts the scheduler processing sample data jobs.
        /// </summary>
        private async Task Run(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                bool waitOnException = false;
                try
                {
                    // Acquire the lease so we can run jobs and calculate the renewal time
                    DateTimeOffset? leaseExpires = await _leaseManager.AcquireLease(cancellationToken).ConfigureAwait(false);

                    // Initialize the jobs since we just acquired the lease
                    await InitializeJobs().ConfigureAwait(false);

                    // Run jobs until we are no longer the lease owner
                    while (leaseExpires.HasValue)
                    {
                        DateTimeOffset renewalTime = leaseExpires.Value.Subtract(TimeBeforeExpirationToRenew);
                        await RunJobsUntil(renewalTime, cancellationToken).ConfigureAwait(false);

                        // Renew the lease (will return null if renewal fails)
                        leaseExpires = await _leaseManager.RenewLease(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected exception, waiting {RetrySeconds} seconds to continue", RetryOnExceptionWaitTime.TotalSeconds);
                    waitOnException = true;
                }

                try
                {
                    // If something bad happened, wait a bit before retrying
                    if (waitOnException)
                        await Task.Delay(RetryOnExceptionWaitTime, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unexpected exception while waiting to try again");
                }
            }
        }

        private async Task InitializeJobs()
        {
            // Initialize all the jobs with their last run time from Cassandra and sort the jobs in place based on their next run time
            await Task.WhenAll(_jobs.Select(j => j.Initialize())).ConfigureAwait(false);
            _jobs.Sort(JobNextRunTimeComparer.Instance);
        }

        private async Task RunJobsUntil(DateTimeOffset leaseRenewalTime, CancellationToken cancellationToken)
        {
            while (DateTimeOffset.UtcNow < leaseRenewalTime)
            {
                // Run a job if ready
                await RunNextJob().ConfigureAwait(false);

                // Wait until the next job needs to run or it's time for us to stop
                await WaitUntilNextJobOrLeaseRenewal(leaseRenewalTime, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Runs the next available job (if any) and returns the run time of the job that will run next.
        /// </summary>
        private async Task RunNextJob()
        {
            // Run the first job in the list
            SampleDataJob job = _jobs[0];
            await job.Run().ConfigureAwait(false);

            // Remove it from the beginning of the list and re-insert it in the correct position based on its new NextRunTime
            _jobs.RemoveAt(0);

            int index = _jobs.BinarySearch(job, JobNextRunTimeComparer.Instance);
            if (index < 0) index = ~index;
            _jobs.Insert(index, job);
        }

        private async Task WaitUntilNextJobOrLeaseRenewal(DateTimeOffset leaseRenewalTime, CancellationToken cancellationToken)
        {
            SampleDataJob nextJob = _jobs[0];
            DateTimeOffset waitUntil = leaseRenewalTime <= nextJob.NextRunTime ? leaseRenewalTime : nextJob.NextRunTime;
            TimeSpan delay = waitUntil - DateTimeOffset.UtcNow;
            if (delay <= TimeSpan.Zero)
                return;

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Comparer that compares jobs based on their next run time.
        /// </summary>
        private class JobNextRunTimeComparer : IComparer<SampleDataJob>
        {
            public static readonly JobNextRunTimeComparer Instance;

            static JobNextRunTimeComparer()
            {
                Instance = new JobNextRunTimeComparer();
            }

            public int Compare(SampleDataJob x, SampleDataJob y)
            {
                return x.NextRunTime.CompareTo(y.NextRunTime);
            }
        }
    }
}
