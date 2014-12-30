using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace KillrVideo.SampleData.Worker.Scheduler
{
    /// <summary>
    /// A scheduler that will run sample data jobs on a schedule.
    /// </summary>
    public class SampleDataJobScheduler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (SampleDataJobScheduler));
        private static readonly TimeSpan RetryOnExceptionWaitTime = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TimeBeforeExpirationToRenew = TimeSpan.FromSeconds(20);

        private readonly LeaseManager _leaseManager;
        private readonly List<SampleDataJob> _jobs;

        public SampleDataJobScheduler(LeaseManager leaseManager, IEnumerable<SampleDataJob> jobs)
        {
            if (leaseManager == null) throw new ArgumentNullException("leaseManager");
            if (jobs == null) throw new ArgumentNullException("jobs");
            _leaseManager = leaseManager;
            _jobs = jobs.ToList();
        }
        
        /// <summary>
        /// Starts the scheduler processing sample data jobs.
        /// </summary>
        public async Task Run(CancellationToken cancellationToken)
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
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unexpected exception.  Waiting {0} seconds to continue.", RetryOnExceptionWaitTime.TotalSeconds), ex);
                    waitOnException = true;
                }

                // If something bad happened, wait a bit before retrying
                if (waitOnException)
                    await Task.Delay(RetryOnExceptionWaitTime, cancellationToken).ConfigureAwait(false);
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
            await job.Run();

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
