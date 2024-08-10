using System.Collections.Concurrent;

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
        instance.SendMessageString("Hello Welcome!!");
    }
}