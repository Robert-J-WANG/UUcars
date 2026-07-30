using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UUcars.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds each authenticated SignalR connection to the group belonging to
    /// its database user when the connection is established.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        // SignalR exposes the user identifier as a string, while UUcars uses
        // integer User IDs. TryParse both converts the value and validates its format.
        if (!int.TryParse(Context.UserIdentifier, out var userId))
        {
            _logger.LogWarning(
                "Authenticated SignalR connection {ConnectionId} has no valid user identifier",
                Context.ConnectionId);

            // Without a valid user ID, this connection cannot safely join a user group.
            Context.Abort();
            return;
        }

        // Every active connection for this user joins the same user-{id} group.
        var groupName = NotificationGroups.ForUser(userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "User {UserId} connection {ConnectionId} joined notification group {GroupName}",
            userId,
            Context.ConnectionId,
            groupName);

        // Continue the normal SignalR connection lifecycle.
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception,
            "Notification connection {ConnectionId} disconnected for user {UserId}",
            Context.ConnectionId,
            Context.UserIdentifier);

        // SignalR automatically removes disconnected connections from groups.
        await base.OnDisconnectedAsync(exception);
    }
}
