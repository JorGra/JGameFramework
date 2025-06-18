using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base component: filters messages, then delegates to concrete UI/logic.
/// </summary>
public abstract class NotificationReceiver : MonoBehaviour
{
    [Header("Filters")]
    [Tooltip("Channels this receiver will accept (case-sensitive).")]
    [SerializeField]
    private List<string> channels =
        new List<string> { "Default" };

    [SerializeField]
    private List<NotificationSeverity> severities =
        new List<NotificationSeverity>
        {
            NotificationSeverity.Info,
            NotificationSeverity.Warning,
            NotificationSeverity.Error
        };

    EventBinding<NotificationEvent> binding;

    protected virtual void OnEnable()
    {
        binding = new EventBinding<NotificationEvent>(OnNotification);
        EventBus<NotificationEvent>.Register(binding);
    }

    protected virtual void OnDisable()
    {
        EventBus<NotificationEvent>.Deregister(binding);
    }

    void OnNotification(NotificationEvent e)
    {
        if (!channels.Contains(e.Message.Channel)) return;
        if (!severities.Contains(e.Message.Severity)) return;

        HandleNotification(e.Message);
    }

    /// <summary>
    /// Implement display logic in subclasses.
    /// </summary>
    protected abstract void HandleNotification(NotificationMessage message);
}
