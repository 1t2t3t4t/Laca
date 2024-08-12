using System.Net.WebSockets;
using System.Threading.Channels;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public interface ISocketInstance : IDisposable
{
    Task Close(WebSocketCloseStatus status, string description);
}

public sealed class SocketInstance : ISocketInstance
{
    public Guid Id { get; }

    private readonly Channel<BaseSocketMessage> _sendingMessageChannel = Channel.CreateUnbounded<BaseSocketMessage>();
    private readonly CancellationTokenSource _cancellationSrc = new();
    private Task? _sendingTask;

    private readonly SocketReader _reader;
    private readonly SocketWriter _writer;
    private readonly WebSocket _webSocket;
    private readonly ISocketManager _socketManager;

    private bool _closeRequested = false;

    public SocketInstance(Guid id, WebSocket webSocket, ISocketManager socketManager)
    {
        Id = id;
        _socketManager = socketManager;
        _webSocket = webSocket;
        _reader = new SocketReader(this, _webSocket, _sendingMessageChannel.Writer, _cancellationSrc.Token);
        _writer = new SocketWriter(this, webSocket, _sendingMessageChannel.Reader, _cancellationSrc.Token);
    }

    public async Task Run()
    {
        _sendingTask = Task.Run(async () =>
        {
            await _writer.Run();
        }, _cancellationSrc.Token);

        await _reader.Run();
    }
    
    public async Task Close(WebSocketCloseStatus status, string description)
    {
        if (_closeRequested) return;

        _closeRequested = true;
        try
        {
            Console.WriteLine($"Closing socket {Id}");
            await _webSocket.CloseAsync(
                status,
                description,
                _cancellationSrc.Token);
            await _cancellationSrc.CancelAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Closing Socket error {e}");
        }
        finally
        {
            _socketManager.DeRegister(this);
        }
    }

    public async void Dispose()
    {
        if (_sendingTask != null)
        {
            await _sendingTask;
            _sendingTask?.Dispose();
        }

        _webSocket.Dispose();
        _cancellationSrc.Dispose();
        _sendingMessageChannel.Writer.Complete();
    }
}