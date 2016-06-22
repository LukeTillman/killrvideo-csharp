using System.Collections.Generic;

namespace KillrVideo.MessageBus.Subscribe
{
    internal class Subscription
    {
        public string Id { get; }
        public List<HandlerRegistration> Handlers { get; } 
        
        public Subscription(string id, List<HandlerRegistration> handlers)
        {
            Id = id;
            Handlers = handlers;
        }
    }
}
