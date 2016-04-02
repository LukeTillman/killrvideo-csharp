using System;

namespace KillrVideo.MessageBus.Subscribe
{
    /// <summary>
    /// A factory for resolving/creating Handler instances and releasing them when finished.
    /// </summary>
    public interface IHandlerFactory
    {
        /// <summary>
        /// Called when initializing subscriptions, this should return all available handler Types that can be resolved
        /// by this factory.
        /// </summary>
        Type[] GetAllHandlerTypes();

        /// <summary>
        /// Resolve a handler instance of Type specified.
        /// </summary>
        THandler Resolve<THandler>();

        /// <summary>
        /// Release the handler instance.
        /// </summary>
        void Release<THandler>(THandler handler);
    }
}