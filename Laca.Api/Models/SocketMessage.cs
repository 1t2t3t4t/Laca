using System.Text.Json.Serialization;

namespace Laca.Api.Models;

public enum SocketAction
{
    CommitMessage
}

public class SocketMessage<T>
{
    public SocketAction Action { get; init; }
    public required T Content { get; init; }
}

public enum Role
{
    User, Bot
}

public class CommitMessage
{
    public Role Role { get; init; }
    public required string Message { get; init; }
}