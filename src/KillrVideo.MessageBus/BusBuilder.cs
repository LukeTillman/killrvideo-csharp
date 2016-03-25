using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;
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
        internal Dictionary<MessageDescriptor, List<IHandlerAdapter>> Handlers { get; }
        internal string SerivceName { get; private set; }

        private BusBuilder()
        {
            Handlers = new Dictionary<MessageDescriptor, List<IHandlerAdapter>>();
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
        /// Subscribe and handle a particular Protobuf message type with the bus.
        /// </summary>
        public BusBuilder Subscribe<T>(MessageDescriptor messageType, IHandleMessage<T> handler)
            where T : IMessage<T>
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            List<IHandlerAdapter> handlers;
            if (Handlers.TryGetValue(messageType, out handlers) == false)
                handlers = new List<IHandlerAdapter>();

            handlers.Add(new HandlerAdapter<T>(handler));
            Handlers[messageType] = handlers;
            return this;
        }

        /// <summary>
        /// Builds a bus instance.
        /// </summary>
        public Bus Build()
        {
            Validate();
            return new Bus(this);
        }

        private void Validate()
        {
            if (SerivceName == null) throw new InvalidOperationException("Must specify a service name for the bus.");
            if (Transport == null) throw new InvalidOperationException("Must specify a transport for the bus.");
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
