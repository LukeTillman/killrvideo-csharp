﻿using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using DryIoc;
using KillrVideo.MessageBus.Subscribe;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// An IHandlerFactory implementation for the message bus that creates handlers from the DryIoc container.
    /// </summary>
    [Export(typeof(IHandlerFactory))]
    public class ContainerHandlerFactory : IHandlerFactory
    {
        private readonly DryIoc.IContainer _container;

        public ContainerHandlerFactory(DryIoc.IContainer container)
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
