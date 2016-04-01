using System;
using KillrVideo.MessageBus.Subscribe;
using KillrVideo.MessageBus.Transport;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// Class for building/configuring a Bus and getting a Bus instance.
    /// </summary>
    public class BusBuilder
    {
        internal IMessageTransport Transport { get; private set; }
        internal string SerivceName { get; private set; }
        internal IHandlerFactory HandlerFactory { get; private set; }

        private BusBuilder()
        {
        }

        /// <summary>
        /// The name of the service this bus is running in.
        /// </summary>
        public BusBuilder WithServiceName(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("Service name cannot be null or whitespace");
            SerivceName = serviceName;
            return this;
        }

        /// <summary>
        /// Configure the message transport to use.
        /// </summary>
        public BusBuilder WithTransport(IMessageTransport transport)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            Transport = transport;
            return this;
        }

        /// <summary>
        /// Configure the handler factory to use.
        /// </summary>
        public BusBuilder WithHandlerFactory(IHandlerFactory handlerFactory)
        {
            if (handlerFactory == null) throw new ArgumentNullException(nameof(handlerFactory));
            HandlerFactory = handlerFactory;
            return this;
        }
        
        /// <summary>
        /// Builds a bus server instance.
        /// </summary>
        public IBusServer Build()
        {
            Validate();
            return new Bus(this);
        }

        private void Validate()
        {
            if (SerivceName == null) throw new InvalidOperationException("Must specify a service name for the bus.");
            if (Transport == null) throw new InvalidOperationException("Must specify a transport for the bus.");
            if (HandlerFactory == null) throw new InvalidOperationException("Must specify a handler factory for the bus");
        }

        /// <summary>
        /// Static method for getting a new BusBuilder instance to configure a bus.
        /// </summary>
        public static BusBuilder Configure()
        {
            return new BusBuilder();
        }
    }
}
