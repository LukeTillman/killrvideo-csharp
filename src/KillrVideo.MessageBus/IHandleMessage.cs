using System.Threading.Tasks;
using Google.Protobuf;

namespace KillrVideo.MessageBus
{
    /// <summary>
    /// Interface for any bus message handlers.
    /// </summary>
    public interface IHandleMessage<in T>
        where T : IMessage<T>
    {
        Task Handle(T message);
    }
}