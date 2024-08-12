using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Laca.Api.Models;

namespace Laca.Api.Socket;

public sealed class SocketReader(
    ISocketInstance socketInstance,
    WebSocket webSocket, 
    ChannelWriter<BaseSocketMessage> sendMessageChannel, 
    CancellationToken cancellationToken)
{
    private const uint BufferSize = 1024 * 4;
    
    public async Task Run()
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await ReceiveMessage();
                switch (message)
                {
                    case SuccessResult success:
                    {
                        await sendMessageChannel.WriteAsync(
                            SocketMessageHelper.CommitMessage(Role.Bot, success.Message),
                            cancellationToken);
                        break;
                    }
                    case CloseResult close:
                    {
                        await socketInstance.Close(close.Status, close.Description);
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            await socketInstance.Close(WebSocketCloseStatus.InternalServerError, $"Socket loop error {e.Message}");
        }
    }
    
    private async Task<ReadResult> ReceiveMessage()
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
}