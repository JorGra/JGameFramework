using System;

/// <summary>
/// Level of importance for a notification.
/// </summary>
public enum NotificationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Immutable data payload for any notification.
/// </summary>
[Serializable]
public struct NotificationMessage
{
    public string Text;
    public NotificationSeverity Severity;
    public string Channel;

    public NotificationMessage(string text,
                               NotificationSeverity severity,
                               string channel)
    {
        Text = text;
        Severity = severity;
        Channel = channel;
    }

    public override string ToString() =>
        $"[{Channel}] {Severity}: {Text}";
}

/// <summary>
/// EventBus wrapper so notifications travel through the global bus.
/// </summary>
public class NotificationEvent : IEvent
{
    public NotificationMessage Message { get; }

    public NotificationEvent(NotificationMessage message)
    {
        Message = message;
    }
}


/// <summary>
/// Static helper for emitting notifications.
/// </summary>
public static class NotificationSender
{
    /// <summary>
    /// Raises a notification immediately via the EventBus.
    /// </summary>
    /// <param name="text">Message body.</param>
    /// <param name="severity">Info / Warning / Error.</param>
    /// <param name="channel">Free-form channel tag (e.g. "UI", "Network").</param>
    public static void Raise(string text,
                             NotificationSeverity severity = NotificationSeverity.Info,
                             string channel = "Default")
    {
        var msg = new NotificationMessage(text, severity, channel);
        EventBus<NotificationEvent>.Raise(new NotificationEvent(msg));
    }
}
