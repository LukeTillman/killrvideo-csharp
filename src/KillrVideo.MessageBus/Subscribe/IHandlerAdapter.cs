using System.Threading.Tasks;
using Google.Protobuf;

namespace KillrVideo.MessageBus.Subscribe
{
    internal interface IHandlerAdapter
    {
        Task Handle(IMessage msg);
    }
}