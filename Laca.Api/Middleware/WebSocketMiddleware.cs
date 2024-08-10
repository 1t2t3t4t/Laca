using System.Net.WebSockets;
using System.Text;
using Laca.Api.Socket;

namespace Laca.Api.Middleware;

public class WebSocketMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var webSocketContext = context.WebSockets;
        if (webSocketContext.IsWebSocketRequest)
        {
            var webSocket = await webSocketContext.AcceptWebSocketAsync();
            var instance = new SocketInstance(webSocket);
            await instance.Run();
        }
        else
        {
            await next(context);
        }
    }
}