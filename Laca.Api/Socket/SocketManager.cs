using System.Collections.Concurrent;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public interface ISocketManager
{
    void Register(SocketInstance instance);
}

public class SocketManager : ISocketManager
{
    private readonly ConcurrentDictionary<Guid, SocketInstance> _connectedInstances = new();

    public void Register(SocketInstance instance)
    {
        if (!_connectedInstances.TryAdd(instance.Id, instance))
        {
            Console.WriteLine($"Duplicate id {instance.Id}");
            return;
        }

        Console.WriteLine("Has connection");
        instance.SendMessage(SocketMessage<CommitMessage>.CommitMessage(new CommitMessage
        {
            Role = Role.Bot,
            Message = "Hello Welcome!!!"
        }));
    }
}