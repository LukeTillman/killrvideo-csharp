using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using KillrVideo.Utils;
using log4net;

namespace KillrVideo.SampleData.Worker.Scheduler
{
    /// <summary>
    /// Component responsible for obtaining/renewing a lease.  Uses Cassandra's LWT to ensure only one worker across the cluster
    /// is the lease owner at any given time.
    /// </summary>
    public class LeaseManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LeaseManager));
        private static readonly TimeSpan MaxLeaseTime = TimeSpan.FromSeconds(180);
        private static readonly TimeSpan RetryOnExceptionWaitTime = TimeSpan.FromSeconds(5);

        private readonly ISession _session;
        private readonly TaskCache<string, PreparedStatement> _statementCache;
        private readonly string _leaseName;
        private readonly string _uniqueId;

        private DateTimeOffset _leaseOwnerUntil;
        
        public LeaseManager(ISession session, TaskCache<string, PreparedStatement> statementCache, LeaseManagerConfig config)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (statementCache == null) throw new ArgumentNullException("statementCache");
            if (config == null) throw new ArgumentNullException("config");
            
            _session = session;
            _statementCache = statementCache;
            _leaseName = config.LeaseName;
            _uniqueId = config.UniqueId;

            // Start by assuming we are not the lease owner
            _leaseOwnerUntil = DateTimeOffset.MinValue;
        }

        /// <summary>
        /// Acquires the lease and returns the time the lease is good until once acquired.
        /// </summary>
        public async Task<DateTimeOffset> AcquireLease(CancellationToken cancellationToken)
        {
            // Use a loop to retry exception failures
            while (true)
            {
                try
                {
                    await AcquireLeaseImpl(cancellationToken).ConfigureAwait(false);
                    return _leaseOwnerUntil;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unexpected exception while acquiring lease. Trying again in {0} seconds.",
                                               RetryOnExceptionWaitTime.TotalSeconds), ex);
                }

                // Wait before trying again (cancellation exceptions are OK here)
                await Task.Delay(RetryOnExceptionWaitTime, cancellationToken).ConfigureAwait(false);
            }
        }
        
        private async Task AcquireLeaseImpl(CancellationToken cancellationToken)
        {
            // If currently the lease owner, nothing to do
            if (IsLeaseOwner())
                return;

            bool acquired;
            DateTimeOffset expiration;
            do
            {
                // Calculate the expiration time in case of success
                expiration = DateTimeOffset.UtcNow.Add(MaxLeaseTime);

                // Try to acquire the lease using LWT in Cassandra
                PreparedStatement prepared =
                    await _statementCache.NoContext.GetOrAddAsync("INSERT INTO sample_data_leases (name, owner) VALUES (?, ?) IF NOT EXISTS");
                BoundStatement bound = prepared.Bind(_leaseName, _uniqueId);
                RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
                Row row = rows.Single();

                acquired = row.GetValue<bool>("[applied]");

                // If we failed, wait until the lease expires to try again
                if (acquired == false)
                    await WaitUntilLeaseExpires(cancellationToken).ConfigureAwait(false);

            } while (acquired == false);
            
            // We succeeded, so remember how long we're the lease owner for
            _leaseOwnerUntil = expiration;
        }

        private async Task WaitUntilLeaseExpires(CancellationToken cancellationToken)
        {
            // Get the next time the lease will expire
            PreparedStatement prepared =
                await _statementCache.NoContext.GetOrAddAsync("SELECT writetime(owner) FROM sample_data_leases WHERE name = ?");
            IStatement bound = prepared.Bind(_leaseName).SetSerialConsistencyLevel(ConsistencyLevel.LocalSerial);
            RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
            Row row = rows.SingleOrDefault();

            // If no row is returned, the lease is expired now
            if (row == null)
                return;

            var expiration = row.GetValue<DateTimeOffset>("writetime(owner)").Add(MaxLeaseTime);

            // See how long we need to wait
            TimeSpan delay = expiration - DateTimeOffset.UtcNow;
            if (delay <= TimeSpan.Zero)
                return;

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to renew the lease and returns the new expiration time if successful, otherwise null.
        /// </summary>
        public async Task<DateTimeOffset?> RenewLease(CancellationToken cancellationToken)
        {
            // Use a loop to retry exception failures
            while (true)
            {
                try
                {
                    bool renewed = await RenewLeaseImpl(cancellationToken).ConfigureAwait(false);
                    return renewed ? _leaseOwnerUntil : (DateTimeOffset?) null;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Unexpected exception while renewing lease. Trying again in {0} seconds.",
                                               RetryOnExceptionWaitTime.TotalSeconds), ex);
                }

                // Wait before trying again (cancellation exceptions are OK here)
                await Task.Delay(RetryOnExceptionWaitTime, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> RenewLeaseImpl(CancellationToken cancellationToken)
        {
            // If we're not the lease owner, we can't renew
            if (IsLeaseOwner() == false)
                return false;

            bool acquired;
            DateTimeOffset expiration;

            do
            {
                // Calculate the expiration time for successful renew
                expiration = DateTimeOffset.UtcNow.Add(MaxLeaseTime);

                // Try to renew the lease using LWT in Cassandra
                PreparedStatement prepared =
                    await _statementCache.NoContext.GetOrAddAsync("UPDATE sample_data_leases SET owner = ? WHERE name = ? IF owner = ?");
                BoundStatement bound = prepared.Bind(_uniqueId, _leaseName, _uniqueId);
                RowSet rows = await _session.ExecuteAsync(bound).ConfigureAwait(false);
                Row row = rows.Single();

                acquired = row.GetValue<bool>("[applied]");

                // If we failed, wait a few seconds and try again
                if (acquired == false)
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

            } while (acquired == false && IsLeaseOwner());

            // If we succeeded, update our state
            if (acquired)
                _leaseOwnerUntil = expiration;

            return acquired;
        }

        /// <summary>
        /// Returns true if we're currently the lease owner.
        /// </summary>
        private bool IsLeaseOwner()
        {
            return _leaseOwnerUntil > DateTimeOffset.UtcNow;
        }
    }
}
