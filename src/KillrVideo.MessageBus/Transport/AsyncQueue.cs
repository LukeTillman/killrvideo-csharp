using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus.Transport
{
    /// <summary>
    /// An async producer/consumer queue.
    /// </summary>
    internal class AsyncQueue<T>
    {
        private readonly Queue<T> _queue;
        private readonly int _capacity;
        private readonly AsyncQueueLock _lock;
        
        protected bool IsFull => _queue.Count == _capacity;
        protected bool HasItems => _queue.Count > 0;

        public AsyncQueue(int capacity)
        {
            _queue = new Queue<T>(capacity);
            _capacity = capacity;
            _lock = new AsyncQueueLock(this);
        }

        public async Task Enqueue(T item, CancellationToken token = default(CancellationToken))
        {
            // Acquire the lock for enqueueing items before modifying the queue
            using (var queueLock = await _lock.GetEnqueueLock(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                _queue.Enqueue(item);
            }
        }

        public async Task<T> Dequeue(CancellationToken token = default(CancellationToken))
        {
            // Acquire the lock for dequeueing items before modifying the queue
            T item;
            using (var queueLock = await _lock.GetDequeueLock(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                item = _queue.Dequeue();
            }

            return item;
        }

        /// <summary>
        /// A coordination helper class that provides coordination of locking around AsyncQueue operations. Favors
        /// dequeueing over enqueueing when there are Tasks waiting.
        /// </summary>
        private class AsyncQueueLock
        {
            private readonly AsyncQueue<T> _queue;

            private readonly Task<Releaser> _fastPathReleaser;

            private readonly Queue<TaskCompletionSource<Releaser>> _waitingEnqueue;
            private readonly Queue<TaskCompletionSource<Releaser>> _waitingDequeue;

            private readonly object _queueLock;

            private int _status;
            
            public AsyncQueueLock(AsyncQueue<T> queue)
            {
                // Keep a reference to the queue so we can know if there are items in it
                _queue = queue;

                // Releaser for fast path
                _fastPathReleaser = Task.FromResult(new Releaser(this));

                // Queues for waiting operations
                _waitingEnqueue = new Queue<TaskCompletionSource<Releaser>>();
                _waitingDequeue = new Queue<TaskCompletionSource<Releaser>>();

                // Object to lock on for synchronization
                _queueLock = new object();

                // Status of lock, 0 = nobody has lock, 1 = enqueue has lock, -1 = dequeue has lock
                _status = 0;
            }

            public Task<Releaser> GetEnqueueLock(CancellationToken token)
            {
                lock (_queueLock)
                {
                    // If no one has the lock and the queue has space
                    if (_status == 0 && _queue.IsFull == false)
                    {
                        // Let the enqueue in right away
                        _status = 1;
                        return _fastPathReleaser;
                    }

                    // Make the enqueue operation wait
                    TaskCompletionSource<Releaser> tcs = CreateTcsInLock(token);
                    _waitingEnqueue.Enqueue(tcs);
                    return tcs.Task;
                }
            }

            public Task<Releaser> GetDequeueLock(CancellationToken token)
            {
                lock (_queueLock)
                {
                    // If no one has the lock and the queue has items in it
                    if (_status == 0 && _queue.HasItems)
                    {
                        // Let the dequeue in right away
                        _status = -1;
                        return _fastPathReleaser;
                    }

                    // Make the dequeue operation wait
                    TaskCompletionSource<Releaser> tcs = CreateTcsInLock(token);
                    _waitingDequeue.Enqueue(tcs);
                    return tcs.Task;
                }
            }

            /// <summary>
            /// Create a new TaskCompletionSource. MUST be executed inside the queue lock.
            /// </summary>
            private TaskCompletionSource<Releaser> CreateTcsInLock(CancellationToken token)
            {
                // Run continuations async so that when setting the result/cancelled, that code doesn't hold onto the lock
                var tcs = new TaskCompletionSource<Releaser>(TaskContinuationOptions.RunContinuationsAsynchronously);

                // If this action runs syncronously (because the token is already cancelled), it should get the lock right away since this
                // method is meant to run inside a locked section of code
                var cancelRegistration = token.Register(() =>
                {
                    lock (_queueLock)
                        tcs.TrySetCanceled(token);
                }, useSynchronizationContext: false);

                // Dispose of the event registration when completed
                tcs.Task.ContinueWith(_ => cancelRegistration.Dispose(), CancellationToken.None);
                return tcs;
            } 

            private void ReleaseLock()
            {
                TaskCompletionSource<Releaser> toWake = null;

                lock (_queueLock)
                {
                    // Are there items in the queue and dequeue operations waiting?
                    if (_queue.HasItems && TryGetNextWaitingOpInLock(_waitingDequeue, out toWake))
                    {
                        // Prepare to wake up the dequeue operation and set the status accordingly
                        _status = -1;
                    }
                    // Otherwise, is there space in the queue and enqueue operations waiting?
                    else if (_queue.IsFull == false && TryGetNextWaitingOpInLock(_waitingEnqueue, out toWake))
                    {
                        // Prepare to wake up the enqueue operation and set the state accordingly
                        _status = 1;
                    }
                    else
                    {
                        // Nobody has the lock
                        _status = 0;
                    }

                    toWake?.SetResult(new Releaser(this));
                }
            }

            /// <summary>
            /// Tries to get the next waiting operation from the specified wait queue. MUST be executed while in the lock.
            /// </summary>
            private bool TryGetNextWaitingOpInLock(Queue<TaskCompletionSource<Releaser>> waitQueue, out TaskCompletionSource<Releaser> waitingOp)
            {
                waitingOp = null;

                // Try to find a waiting op that isn't completed
                while (waitQueue.Count > 0)
                {
                    // Just throw out waiting ops that have already been completed (cancelled)
                    TaskCompletionSource<Releaser> tcs = waitQueue.Dequeue();
                    if (tcs.Task.IsCompleted)
                        continue;

                    waitingOp = tcs;
                    return true;
                }

                return false;
            }
            
            /// <summary>
            /// Disposable struct so lock can be used with using( ... ) pattern to release the lock on Dispose().
            /// </summary>
            public struct Releaser : IDisposable
            {
                private readonly AsyncQueueLock _toRelease;

                public Releaser(AsyncQueueLock toRelease)
                {
                    _toRelease = toRelease;
                }

                public void Dispose()
                {
                    _toRelease?.ReleaseLock();
                }
            }
        }
    }
}