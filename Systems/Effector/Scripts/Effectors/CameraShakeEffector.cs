using System.Collections;
using UnityEngine;
using JG.Tools;  // Adjust namespace if needed

/// <summary>
/// Sits on the Camera. Listens for CameraShakeEvent and applies the shake.
/// Always returns to the original local transform.
/// </summary>
public class CameraShakeEffector : MonoBehaviour
{
    // The coroutine currently handling shake
    private Coroutine shakeCoroutine;

    // Last offset we applied; lets us remove only the shake without fighting the controller
    private Vector3 lastOffset = Vector3.zero;
    private Quaternion lastRotOffset = Quaternion.identity;

    private void OnEnable()
    {
        this.SubscribeEvent<CameraShakeEvent>(OnCameraShake);
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
            // Remove the previous shake offset but keep whatever the controller set
            ResetOffsets();
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
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Extract the "controller driven" pose by removing the last shake offsets
            Vector3 baseLocalPos = transform.localPosition - lastOffset;
            Quaternion baseLocalRot = transform.localRotation * Quaternion.Inverse(lastRotOffset);

            // Random offset for position
            float offsetX = Random.Range(-1f, 1f) * intensity;
            float offsetY = Random.Range(-1f, 1f) * intensity;

            // Example: small random rotation around z-axis
            float angleZ = Random.Range(-1f, 1f) * intensity * 5f; // tweak multiplier as needed

            // Apply position & rotation
            Vector3 newOffset = new Vector3(offsetX, offsetY, 0f);
            Quaternion newRotOffset = Quaternion.Euler(0f, 0f, angleZ);
            transform.localPosition = baseLocalPos + newOffset;
            transform.localRotation = baseLocalRot * newRotOffset;

            lastOffset = newOffset;
            lastRotOffset = newRotOffset;

            // Wait for 'frequency' each step
            yield return new WaitForSeconds(frequency);
            elapsed += frequency;
        }

        // Remove only the shake we applied; camera controller continues to drive the base pose
        ResetOffsets();

        shakeCoroutine = null;
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        ResetOffsets();
    }

    private void ResetOffsets()
    {
        // Strip the last applied shake without snapping to any cached "origin"
        transform.localPosition -= lastOffset;
        transform.localRotation *= Quaternion.Inverse(lastRotOffset);
        lastOffset = Vector3.zero;
        lastRotOffset = Quaternion.identity;
    }
}
