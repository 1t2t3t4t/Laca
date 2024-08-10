using System.Collections.Concurrent;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public interface ISocketManager
{
    void Register(SocketInstance instance);
    void DeRegister(SocketInstance instance);
}

public class SocketManager : ISocketManager
{
    // Socket Guid -> Socket Instance
    private readonly ConcurrentDictionary<Guid, SocketInstance> _connectedInstances = new();

    public async void Register(SocketInstance instance)
    {
        if (!_connectedInstances.TryAdd(instance.Id, instance))
        {
            Console.WriteLine($"Duplicate id {instance.Id}");
            return;
        }

        Console.WriteLine($"Has connection {instance.Id}");
        await instance.SendMessage(SocketMessageHelper.CommitMessage(new CommitMessage
        {
            Role = Role.Bot,
            Message = "Hello Welcome!!!"
        }));
    }

    public void DeRegister(SocketInstance instance)
    {
        _connectedInstances.Remove(instance.Id, out _);
        Console.WriteLine($"DeRegister {instance.Id}");
    }
}