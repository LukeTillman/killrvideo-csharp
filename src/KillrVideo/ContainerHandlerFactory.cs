using System;
using System.ComponentModel.Composition;
using System.Linq;
using DryIoc;
using KillrVideo.MessageBus;
using KillrVideo.MessageBus.Subscribe;

namespace KillrVideo
{
    /// <summary>
    /// An IHandlerFactory implementation for the message bus that creates handlers from the DryIoc container.
    /// </summary>
    [Export(typeof(IHandlerFactory))]
    public class ContainerHandlerFactory : IHandlerFactory
    {
        private readonly IContainer _container;

        public ContainerHandlerFactory(IContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            _container = container;
        }

        public Type[] GetAllHandlerTypes()
        {
            return _container.GetServiceRegistrations()
                             .Where(sr => sr.ServiceType.IsMessageHandlerInterface())
                             .Select(sr => sr.ServiceType)
                             .ToArray();
        }

        public THandler Resolve<THandler>()
        {
            return _container.Resolve<THandler>();
        }

        public void Release<THandler>(THandler handler)
        {
            var disposableHandler = handler as IDisposable;
            disposableHandler?.Dispose();
        }
    }
}
