namespace Laca.Api.Models;

public static class SocketMessageHelper
{
    public static SocketMessage<CommitMessage> CommitMessage(Role role, string message)
    {
        return new SocketMessage<CommitMessage>
        {
            Action = SocketAction.CommitMessage,
            DynContent = new CommitMessage
            {
                Role = role,
                Message = message
            }
        };
    }
}