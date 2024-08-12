namespace Laca.Api.Models;

public enum SocketAction
{
    CommitMessage
}

public abstract record BaseSocketMessage
{
    public SocketAction Action { get; init; }
    public abstract object Content { get; }
}

public sealed record SocketMessage<T> : BaseSocketMessage where T : class
{
    public required T DynContent { get; init; }
    public override object Content => DynContent;
}

public enum Role
{
    User, Bot
}

public record CommitMessage
{
    public Role Role { get; init; }
    public required string Message { get; init; }
}