namespace UUcars.API.Hubs;

/// <summary>
/// Centralises the SignalR group names used by the notification system so
/// connections and notification delivery always use the same naming format.
/// </summary>
public static class NotificationGroups
{
    /// <summary>
    /// Returns the notification group name for a database user ID.
    /// For example, user ID 5 maps to "user-5".
    /// </summary>
    public static string ForUser(int userId)
    {
        return $"user-{userId}";
    }
}
