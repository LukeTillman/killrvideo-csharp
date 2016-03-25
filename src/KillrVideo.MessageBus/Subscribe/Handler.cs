using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace KillrVideo.MessageBus.Subscribe
{
    /// <summary>
    /// Helper class to take a generic IHandleMessage&lt;T&gt; and convert it to a handler that will take
    /// a non-generic IMessage.
    /// </summary>
    internal class HandlerAdapter<T> : IHandlerAdapter
        where T : IMessage<T>
    {
        private readonly IHandleMessage<T> _handler;

        public HandlerAdapter(IHandleMessage<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handler = handler;
        }

        public Task Handle(IMessage msg)
        {
            var typedMessage = (T) msg;
            return _handler.Handle(typedMessage);
        }
    }
}
