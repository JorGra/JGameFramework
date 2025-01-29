using System.Collections;
using UnityEngine;
using JG.Tools;  // Adjust namespace if needed

/// <summary>
/// Sits on the Camera. Listens for CameraShakeEvent and applies the shake.
/// Always returns to the original local transform.
/// </summary>
public class CameraShakeEffector : MonoBehaviour
{
    // Event bus binding
    private IEventBinding<CameraShakeEvent> shakeEventBinding;

    // The coroutine currently handling shake
    private Coroutine shakeCoroutine;

    // Store the camera's default local transform
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;

    private void Awake()
    {
        // Capture our "resting" local transform
        defaultLocalPosition = transform.localPosition;
        defaultLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        // Register for the camera shake event
        shakeEventBinding = new EventBinding<CameraShakeEvent>(OnCameraShake);
        EventBus<CameraShakeEvent>.Register(shakeEventBinding);
    }

    private void OnDisable()
    {
        // Deregister to avoid leaks
        EventBus<CameraShakeEvent>.Deregister(shakeEventBinding);
    }

    /// <summary>
    /// Called whenever a CameraShakeEvent is raised.
    /// </summary>
    private void OnCameraShake(CameraShakeEvent shakeEvent)
    {
        // If we are already shaking, decide whether to stop or stack shakes.
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
            // Reset to default before starting a new shake
            transform.localPosition = defaultLocalPosition;
            transform.localRotation = defaultLocalRotation;
        }

        // Start a new shake
        shakeCoroutine = StartCoroutine(
            ShakeCoroutine(shakeEvent.Intensity, shakeEvent.Frequency, shakeEvent.Duration)
        );
    }

    /// <summary>
    /// The shaking logic. Moves and/or rotates the camera around its original local transform.
    /// </summary>
    private IEnumerator ShakeCoroutine(float intensity, float frequency, float duration)
    {
        // Cache the original local transform at shake start (could be the default or partially offset).
        Vector3 startLocalPos = transform.localPosition;
        Quaternion startLocalRot = transform.localRotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Random offset for position
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            // Example: small random rotation around z-axis
            float angleZ = Random.Range(-1f, 1f) * intensity * 5f; // tweak multiplier as needed

            // Apply position & rotation
            transform.localPosition = startLocalPos + new Vector3(offsetX, offsetY, 0f);
            transform.localRotation = startLocalRot * Quaternion.Euler(0f, 0f, angleZ);

            // Wait for 'frequency' each step
            yield return new WaitForSeconds(frequency);
            elapsed += frequency;
        }

        // Reset to the default local transform
        transform.localPosition = defaultLocalPosition;
        transform.localRotation = defaultLocalRotation;

        shakeCoroutine = null;
    }
}
