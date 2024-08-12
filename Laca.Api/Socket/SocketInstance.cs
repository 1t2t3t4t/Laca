using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public sealed class SocketInstance(Guid id, WebSocket webSocket, ISocketManager socketManager) : IDisposable
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

    private readonly Channel<string> _sendingMessageChannel = Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _cancellationSrc = new();
    private Task? _sendingTask;
    
    public async Task Run()
    {
        _sendingTask = Task.Run(async () =>
        {
            try
            {
                await SendingLoop(_cancellationSrc.Token);
            }
            catch (Exception e)
            {
                await Close(WebSocketCloseStatus.InternalServerError, $"Socket sending error {e}");
            }
            Console.WriteLine("Ended sending loop");
        }, _cancellationSrc.Token);

        try
        {
            while (!_cancellationSrc.IsCancellationRequested)
            {
                var message = await ReceiveMessage(_cancellationSrc.Token);
                switch (message)
                {
                    case SuccessResult success:
                    {
                        await SendMessage(SocketMessageHelper.CommitMessage(Role.Bot, success.Message));
                        break;
                    }
                    case CloseResult close:
                    {
                        await Close(close.Status, close.Description);
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Closing socket {id}. Socket loop error {e}");
            await Close(WebSocketCloseStatus.InternalServerError, $"Socket loop error {e}");
            await _cancellationSrc.CancelAsync();
        }
    }

    private async Task SendingLoop(CancellationToken cancellationToken)
    {
        while (!_cancellationSrc.IsCancellationRequested)
        {
            var msg = await _sendingMessageChannel.Reader.ReadAsync(cancellationToken);
            if (msg.Contains("Error"))
            {
                throw new Exception("Test error");
            }
            var textBytes = Encoding.UTF8.GetBytes(msg);
            await webSocket.SendAsync(textBytes, WebSocketMessageType.Text, true, cancellationToken);
        }
    }
    
    private async Task Close(WebSocketCloseStatus status, string description)
    {
        if (_cancellationSrc.IsCancellationRequested) return;
        
        try
        {
            Console.WriteLine($"Closing socket {id}");
            await _cancellationSrc.CancelAsync();
            await webSocket.CloseAsync(
                status,
                description,
                CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Closing Socket error {e}");
        }
        finally
        {
            _sendingMessageChannel.Writer.Complete();
            socketManager.DeRegister(this);
        }
    }

    public async Task SendMessage<T>(SocketMessage<T> message)
    {
        var cancellationToken = _cancellationSrc.Token;
        using var memStream = new MemoryStream();
        
        await JsonSerializer.SerializeAsync(memStream, message, SerializerOptions, cancellationToken);
        memStream.Seek(0, SeekOrigin.Begin);
        
        using var reader = new StreamReader(memStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(cancellationToken);
        await SendMessageString(content);
    }

    private async Task SendMessageString(string text)
    {
        await _sendingMessageChannel.Writer.WriteAsync(text);
    }

    private async Task<ReadResult> ReceiveMessage(CancellationToken cancellationToken)
    {
        var resultString = "";
        var buffer = new byte[BufferSize];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), cancellationToken);

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
                new ArraySegment<byte>(buffer), cancellationToken);
        }

        if (receiveResult.CloseStatus.HasValue)
        {
            return new CloseResult(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription ?? "NULL");
        }

        return new SuccessResult(resultString);
    }

    public async void Dispose()
    {
        if (_sendingTask != null)
        {
            await _sendingTask;
            _sendingTask?.Dispose();
        }

        webSocket.Dispose();
        _cancellationSrc.Dispose();
    }
}