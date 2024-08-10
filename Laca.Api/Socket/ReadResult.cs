using System.Net.WebSockets;

namespace Laca.Api.Socket;

internal abstract record ReadResult;

internal record SuccessResult(string Message) : ReadResult;

internal record CloseResult(WebSocketCloseStatus Status, string Description) : ReadResult;