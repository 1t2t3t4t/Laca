using System.Net.WebSockets;
using System.Text;
using Laca.Api.Socket;

namespace Laca.Api.Middleware;

public class WebSocketMiddleware(ISocketManager socketManager) : IMiddleware
{
    private ISocketManager SocketManager { get; } = socketManager;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var webSocketContext = context.WebSockets;
        if (webSocketContext.IsWebSocketRequest)
        {
            var webSocket = await webSocketContext.AcceptWebSocketAsync();
            var instance = new SocketInstance(Guid.NewGuid(), webSocket, SocketManager);
            SocketManager.Register(instance);
            await instance.Run();
        }
        else
        {
            await next(context);
        }
    }
}