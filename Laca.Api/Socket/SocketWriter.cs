using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public class SocketWriter(
    ISocketInstance socketInstance,
    WebSocket webSocket,
    ChannelReader<BaseSocketMessage> sendingMessageChannel,
    CancellationToken cancellationToken)
{
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
    
    public async Task Run()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await sendingMessageChannel.ReadAsync(cancellationToken);
                var msgString = await ParseMessage(msg);
                var textBytes = Encoding.UTF8.GetBytes(msgString);
                await webSocket.SendAsync(textBytes, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
        catch (Exception e)
        {
            await socketInstance.Close(WebSocketCloseStatus.InternalServerError, $"Socket sending error {e.Message}");
        }
    }
    
    private async Task<string> ParseMessage(BaseSocketMessage message)
    {
        using var memStream = new MemoryStream();
        
        await JsonSerializer.SerializeAsync(memStream, message, SerializerOptions, cancellationToken);
        memStream.Seek(0, SeekOrigin.Begin);
        
        using var reader = new StreamReader(memStream, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}