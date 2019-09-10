using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Serilog;
using Serilog.Events;

namespace KillrVideo.MessageBus.Subscribe
{
    /// <summary>
    /// Represents a handler registered with the bus.
    /// </summary>
    internal abstract class HandlerRegistration
    {
        protected static readonly ILogger Logger = Log.ForContext<HandlerRegistration>();

        /// <summary>
        /// The MessageDescriptor handled by the handler.
        /// </summary>
        public MessageDescriptor MessageDescriptor { get; }

        protected HandlerRegistration(MessageDescriptor messageDescriptor)
        {
            MessageDescriptor = messageDescriptor;
        }

        public abstract Task Dispatch(IHandlerFactory factory, byte[] message);


        /// <summary>
        /// Static factory method for creating a HandlerRegistration for the given Handler Type and Message Type.
        /// </summary>
        public static HandlerRegistration Create<THandler, TMessage>()
            where THandler : IHandleMessage<TMessage>
            where TMessage : IMessage, new()
        {
            return new HandlerRegistration<THandler,TMessage>(new TMessage().Descriptor);
        }
    }

    internal class HandlerRegistration<THandler, TMessage> : HandlerRegistration
        where THandler : IHandleMessage<TMessage>
        where TMessage : IMessage, new()
    {
        public HandlerRegistration(MessageDescriptor messageDescriptor) 
            : base(messageDescriptor)
        {
        }

        public override async Task Dispatch(IHandlerFactory factory, byte[] message)
        {
            THandler handler = default(THandler);
            try
            {
                var msg = (TMessage) MessageDescriptor.Parser.ParseFrom(message);

                // Log the message if debug logging is enabled
                if (Logger.IsEnabled(LogEventLevel.Debug))
                {
                    Logger.Debug("Dispatch message {FullName} {JsonMessageString}", msg.Descriptor.FullName, msg.ToString());
                }

                handler = factory.Resolve<THandler>();
                await handler.Handle(msg).ConfigureAwait(false);
            }
            finally
            {
                if (Equals(handler, default(THandler)) == false)
                    factory.Release(handler);
            }
        }
    }
}
