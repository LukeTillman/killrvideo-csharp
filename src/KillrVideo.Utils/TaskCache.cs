using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KillrVideo.Utils
{
    /// <summary>
    /// A cache for async operation results.  Pass an async factory function (i.e. one that returns a Task&lt;TItem&gt;) for generating items from a key 
    /// to the constructor.  The cache will ensure that only one Task is ever created for a given key (i.e. that the factory func will only ever be
    /// invoked once for a given key) UNLESS the Task for a key is faulted, in which case a new one will be generated.
    /// </summary>
    /// <typeparam name="TKey">The key type for items in the cache</typeparam>
    /// <typeparam name="TItem">The item type of items in the cache</typeparam>
    public class TaskCache<TKey, TItem> : INoCapturedContextTaskCache<TKey, TItem>
    {
        private readonly Func<TKey, Task<TItem>> _factoryFunc;
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TItem>>> _cachedItems;

        /// <summary>
        /// Gets the TaskCache with an API that won't capture the Task context.  Useful to avoid having to put ConfigureAwait(false) all
        /// over your code.
        /// </summary>
        public INoCapturedContextTaskCache<TKey, TItem> NoContext
        {
            get { return this; }
        }

        public TaskCache(Func<TKey, Task<TItem>> factoryFunc)
        {
            if (factoryFunc == null) throw new ArgumentNullException("factoryFunc");
            _factoryFunc = factoryFunc;

            _cachedItems = new ConcurrentDictionary<TKey, Lazy<Task<TItem>>>();
        }

        /// <summary>
        /// Gets the Task for the given key from the cache or adds it by invoking the factory function.  Returns the Task.
        /// </summary>
        public Task<TItem> GetOrAddAsync(TKey key)
        {
            return _cachedItems.AddOrUpdate(key, CreateTask, UpdateTaskIfFaulted).Value;
        }

        ConfiguredTaskAwaitable<TItem> INoCapturedContextTaskCache<TKey, TItem>.GetOrAddAsync(TKey key)
        {
            return GetOrAddAsync(key).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the Tasks for the given keys from the cache or adds them by invoking the factory function.  Returns a Task
        /// that can be awaited for when all items Tasks are complete.
        /// </summary>
        public Task<TItem[]> GetOrAddAllAsync(params TKey[] keys)
        {
            if (keys == null || keys.Length == 0)
                throw new ArgumentException("You must specify at least one key to get.", "keys");
            
            return Task.WhenAll(keys.Select(GetOrAddAsync));
        }

        ConfiguredTaskAwaitable<TItem[]> INoCapturedContextTaskCache<TKey, TItem>.GetOrAddAllAsync(params TKey[] keys)
        {
            return GetOrAddAllAsync(keys).ConfigureAwait(false);
        }

        private Lazy<Task<TItem>> CreateTask(TKey key)
        {
            return new Lazy<Task<TItem>>(() => _factoryFunc(key));
        }

        private Lazy<Task<TItem>> UpdateTaskIfFaulted(TKey key, Lazy<Task<TItem>> currentValue)
        {
            // If the cached task is faulted, create a new one, otherwise just return the current value
            return currentValue.Value.IsFaulted ? CreateTask(key) : currentValue;
        }
    }
}
