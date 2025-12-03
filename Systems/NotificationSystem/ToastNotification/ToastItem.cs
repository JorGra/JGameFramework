using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles the visual lifecycle of a single toast.
/// </summary>
public class ToastItem : MonoBehaviour
{
    [SerializeField] private TMP_Text textField;
    [SerializeField] private CanvasGroup canvasGroup;

    /// <summary>
    /// Populate UI and start fade in/out coroutine.
    /// </summary>
    public void Initialize(NotificationMessage message,
                           float lifetime,
                           float fadeDuration)
    {
        if (textField != null) textField.text = message.Text;
        StartCoroutine(Animate(lifetime, fadeDuration));
    }

    IEnumerator Animate(float lifetime, float fadeDuration)
    {
        // Fade-in
        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Stay visible
        yield return new WaitForSecondsRealtime(lifetime);

        // Fade-out
        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            canvasGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
