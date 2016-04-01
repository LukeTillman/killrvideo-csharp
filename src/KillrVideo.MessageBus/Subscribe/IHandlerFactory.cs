namespace KillrVideo.MessageBus.Subscribe
{
    /// <summary>
    /// A factory for resolving/creating Handler instances and releasing them when finished.
    /// </summary>
    public interface IHandlerFactory
    {
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