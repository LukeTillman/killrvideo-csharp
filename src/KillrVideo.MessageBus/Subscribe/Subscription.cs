using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace KillrVideo.MessageBus.Subscribe
{
    internal class Subscription
    {
        public string Id { get; }
        public MessageDescriptor MessageType { get; }

        private readonly List<IHandlerAdapter> _handlers;

        public Subscription(string id, MessageDescriptor messageType, List<IHandlerAdapter> handlers)
        {
            Id = id;
            MessageType = messageType;
            _handlers = handlers;
        }

        public Task DispatchMessage(IMessage message)
        {
            var tasks = _handlers.Select(h => h.Handle(message));
            return Task.WhenAll(tasks);
        }
    }
}
