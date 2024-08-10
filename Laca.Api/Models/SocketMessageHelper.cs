namespace Laca.Api.Models;

public static class SocketMessageHelper
{
    public static SocketMessage<CommitMessage> CommitMessage(CommitMessage content)
    {
        return new SocketMessage<CommitMessage>
        {
            Action = SocketAction.CommitMessage,
            Content = content
        };
    }
}