using Laca.Api.Socket;
using Microsoft.AspNetCore.Mvc;

namespace Laca.Api.Controllers;

[Route("/socket")]
public class SocketController(ISocketManager socketManager) : ControllerBase
{
    [HttpGet("/connections")]
    public IEnumerable<Guid> GetConnectionList()
    {
        return socketManager.GetCurrentConnections();
    }
}