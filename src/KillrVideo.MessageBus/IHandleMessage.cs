using System.Threading.Tasks;
using Google.Protobuf;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// Interface for message handlers.
    /// </summary>
    public interface IHandleMessage<in T> where T : IMessage, new()
    {
        Task Handle(T message);
    }
}