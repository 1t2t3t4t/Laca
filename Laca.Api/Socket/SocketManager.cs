using System.Collections.Concurrent;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public interface ISocketManager
{
    IEnumerable<Guid> GetCurrentConnections();
    
    void Register(SocketInstance instance);
    void DeRegister(SocketInstance instance);
}

public class SocketManager : ISocketManager
{
    // Socket Guid -> Socket Instance
    private readonly ConcurrentDictionary<Guid, SocketInstance> _connectedInstances = new();

    public IEnumerable<Guid> GetCurrentConnections() => _connectedInstances.Keys;

    public async void Register(SocketInstance instance)
    {
        if (!_connectedInstances.TryAdd(instance.Id, instance))
        {
            Console.WriteLine($"Duplicate id {instance.Id}");
            return;
        }

        Console.WriteLine($"Has connection {instance.Id}");
    }

    public void DeRegister(SocketInstance instance)
    {
        if (_connectedInstances.Remove(instance.Id, out var removed))
        {
            removed.Dispose();
        }
        Console.WriteLine($"DeRegister {instance.Id}");
    }
}