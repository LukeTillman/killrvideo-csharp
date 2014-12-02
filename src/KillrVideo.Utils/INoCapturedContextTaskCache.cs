using System.Runtime.CompilerServices;

namespace KillrVideo.Utils
{
    /// <summary>
    /// A TaskCache where Task context is not captured (i.e. ConfigureAwait(false) is set).
    /// </summary>
    public interface INoCapturedContextTaskCache<in TKey, TItem>
    {
        /// <summary>
        /// Gets the Task for the given key from the cache or adds it by invoking the factory function.  Returns the Task.
        /// </summary>
        ConfiguredTaskAwaitable<TItem> GetOrAddAsync(TKey key);

        /// <summary>
        /// Gets the Tasks for the given keys from the cache or adds them by invoking the factory function.  Returns a Task
        /// that can be awaited for when all items Tasks are complete.
        /// </summary>
        ConfiguredTaskAwaitable<TItem[]> GetOrAddAllAsync(params TKey[] keys);
    }
}