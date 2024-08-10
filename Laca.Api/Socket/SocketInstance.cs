using System.Net.WebSockets;
using System.Text;

namespace Laca.Api.Socket;

public class SocketInstance(Guid id, WebSocket webSocket)
{
    private const uint BufferSize = 1024 * 4;

    public Guid Id => id;
    
    public async Task Run()
    {
        while (true)
        {
            var message = await ReceiveMessage();
            switch (message)
            {
                case SuccessResult success:
                {
                    await SendMessageString(success.Message);
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
    
    private async Task Close(WebSocketCloseStatus status, string description)
    {
        await webSocket.CloseAsync(
            status,
            description,
            CancellationToken.None);
    }

    public async Task SendMessageString(string text)
    {
        var textBytes = Encoding.UTF8.GetBytes(text);
        await webSocket.SendAsync(textBytes, WebSocketMessageType.Text, true, CancellationToken.None);
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