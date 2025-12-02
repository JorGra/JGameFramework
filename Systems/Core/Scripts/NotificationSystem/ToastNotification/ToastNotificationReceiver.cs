using UnityEngine;

/// <summary>
/// Spawns fading toast UI elements for each notification.
/// </summary>
public class ToastNotificationReceiver : NotificationReceiver
{
    [Header("Toast Setup")]
    [SerializeField] private RectTransform toastContainer;
    [SerializeField] private GameObject toastPrefab;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float fadeDuration = 0.25f;

    protected override void HandleNotification(NotificationMessage message)
    {
        if (toastPrefab == null || toastContainer == null) return;

        var go = Instantiate(toastPrefab, toastContainer);
        var item = go.GetComponent<ToastItem>();
        if (item != null) item.Initialize(message, lifetime, fadeDuration);
    }
}
