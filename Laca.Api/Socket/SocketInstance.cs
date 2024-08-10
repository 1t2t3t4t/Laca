using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public class SocketInstance(Guid id, WebSocket webSocket, ISocketManager socketManager)
{
    private const uint BufferSize = 1024 * 4;
    
    private static readonly JsonNamingPolicy NamingPolicy = JsonNamingPolicy.CamelCase;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = NamingPolicy,
        Converters =
        {
            new JsonStringEnumConverter<Role>(NamingPolicy),
            new JsonStringEnumConverter<SocketAction>(NamingPolicy)
        }
    };
    
    public Guid Id => id;
    public bool Closed { get; private set; } = false;

    private readonly ConcurrentQueue<string> _pendingPushMessages = new();
    private Task? _sendingTask;
    
    public async Task Run()
    {
        _sendingTask = Task.Run(async () =>
        {
            while (!Closed)
            {
                while (_pendingPushMessages.TryDequeue(out var msg))
                {
                    var textBytes = Encoding.UTF8.GetBytes(msg);
                    await webSocket.SendAsync(textBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        });

        while (true)
        {
            var message = await ReceiveMessage();
            switch (message)
            {
                case SuccessResult success:
                {
                    await Task.Delay(300);
                    await SendMessage(SocketMessageHelper.CommitMessage(Role.Bot, success.Message));
                    break;
                }
                case CloseResult close:
                {
                    await Close(close.Status, close.Description);
                    _sendingTask = null;
                    return;
                }
            }
        }
    }
    
    private async Task Close(WebSocketCloseStatus status, string description)
    {
        Closed = true;
        if (_sendingTask != null) await _sendingTask;
        await webSocket.CloseAsync(
            status,
            description,
            CancellationToken.None);
        socketManager.DeRegister(this);
    }

    public async Task SendMessage<T>(SocketMessage<T> message)
    {
        using var memStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memStream, message, SerializerOptions);
        memStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(memStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        SendMessageString(content);
    }

    private void SendMessageString(string text)
    {
        _pendingPushMessages.Enqueue(text);
    }

    private async Task<ReadResult> ReceiveMessage()
    {
        var resultString = "";
        var buffer = new byte[BufferSize];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            resultString += Encoding.UTF8.GetString(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count));
            if (receiveResult.EndOfMessage)
            {
                break;
            }
            
            buffer = new byte[BufferSize];
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        if (receiveResult.CloseStatus.HasValue)
        {
            return new CloseResult(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription ?? "NULL");
        }

        return new SuccessResult(resultString);
    }
}