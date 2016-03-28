using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using Serilog;

namespace KillrVideo.SampleData.Scheduler
{
    /// <summary>
    /// Represents a sample data job that needs to run on a schedule.
    /// </summary>
    public abstract class SampleDataJob
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan TimeUntilRetry = TimeSpan.FromSeconds(5);
        
        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly ILogger _logger;
        private readonly string _jobName;

        private DateTimeOffset _nextScheduledRunTime;

        /// <summary>
        /// The number of minutes between job runs.
        /// </summary>
        protected abstract int MinutesBetweenRuns { get; }

        /// <summary>
        /// Whether or not this is the first time the job has been run.
        /// </summary>
        protected bool IsFirstTimeRunning { get; private set; }

        /// <summary>
        /// The next time the job will run.
        /// </summary>
        public DateTimeOffset NextRunTime { get; private set; }
        
        protected SampleDataJob(ISession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _session = session;

            _statementCache = new TaskCache<string, PreparedStatement>(_session.PrepareAsync);
            _jobName = GetType().FullName;
            _logger = Log.ForContext(GetType());
            NextRunTime = DateTimeOffset.MaxValue;
        }

        /// <summary>
        /// Initializes a job.  Will calculate the NextRunTime value based on data logged to Cassandra about when a job was last run.
        /// </summary>
        public async Task Initialize()
        {
            // Lookup the last run time in Cassandra
            PreparedStatement prepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT scheduled_run_time FROM sample_data_job_log WHERE job_name = ? LIMIT 1");
            BoundStatement bound = prepared.Bind(_jobName);
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();

            // Calculate the next scheduled run time based on the last run time
            var lastRunTime = row?.GetValue<DateTimeOffset>("scheduled_run_time");
            SetNextRunTime(lastRunTime);
        }

        /// <summary>
        /// Runs a job if it's time to run and updates the NextRunTime property value appropriately.  Will throw if not initialized previously.
        /// </summary>
        public async Task Run()
        {
            if (NextRunTime == DateTimeOffset.MaxValue)
                throw new InvalidOperationException("Job is not initialized");

            // If for some reason this is called before it's time to run, just bail
            if (NextRunTime > DateTimeOffset.UtcNow)
                return;

            try
            {
                // Run the job, then log the run in Cassandra
                await RunImpl().ConfigureAwait(false);
                PreparedStatement prepared = await _statementCache.NoContext.GetOrAddAsync(
                    "INSERT INTO sample_data_job_log (job_name, scheduled_run_time, actual_run_time) VALUES (?, ?, ?)");
                BoundStatement bound = prepared.Bind(_jobName, _nextScheduledRunTime, DateTimeOffset.UtcNow);
                await _session.ExecuteAsync(bound).ConfigureAwait(false);

                // Calculate a new run time based on the current one
                SetNextRunTime(_nextScheduledRunTime);
            }
            catch (Exception ex)
            {
                // Log the error and set the next run time to wait a bit before retrying
                _logger.Error(ex, "Exception while executing job, retry in {RetrySeconds} seconds", TimeUntilRetry.TotalSeconds);
                NextRunTime = NextRunTime.Add(TimeUntilRetry);
            }
        }

        /// <summary>
        /// Should do the actual work of the job.
        /// </summary>
        protected abstract Task RunImpl();
        
        /// <summary>
        /// Sets the next run time and next scheduled run time based on the last scheduled run time that was successful.
        /// </summary>
        private void SetNextRunTime(DateTimeOffset? lastScheduledRunTime)
        {
            var now = DateTimeOffset.UtcNow;

            // Calculate the most recent time in the past that the job would have run if it had started at Epoch time
            double minutesSinceEpoch = now.Subtract(Epoch).TotalMinutes;
            double lastRunMinutesSinceEpoch = Math.Floor(minutesSinceEpoch / MinutesBetweenRuns) * MinutesBetweenRuns;
            DateTimeOffset lastTimeShouldHaveRun = Epoch.AddMinutes(lastRunMinutesSinceEpoch);
            
            // If the last scheduled time it ran is null (i.e. this is the first time it's ever been run), or it missed at least
            // one run in the past, run it immediately for the most recent run it missed (this means jobs can skip runs when the
            // service is shutdown which is OK, especially for local developer machines where the service may be shut off for
            // long periods of time)
            if (lastScheduledRunTime == null || lastScheduledRunTime.Value != lastTimeShouldHaveRun)
            {
                _nextScheduledRunTime = lastTimeShouldHaveRun;
                NextRunTime = now;
            }
            else
            {
                // It ran the last time it should have, so just increment to the next time
                _nextScheduledRunTime = lastTimeShouldHaveRun.AddMinutes(MinutesBetweenRuns);
                NextRunTime = _nextScheduledRunTime;
            }
            
            // If we don't have a last scheduled run time, this is the first time the job has run
            IsFirstTimeRunning = lastScheduledRunTime.HasValue == false;
        }
    }
}