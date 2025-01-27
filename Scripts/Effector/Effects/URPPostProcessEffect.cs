using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Example effect that modifies a URP post-processing Volume override
/// (e.g., bloom intensity) for a certain duration.
/// </summary>
public class URPPostProcessEffect : EffectBase
{
    [Header("Post Processing Settings")]
    public Volume volume; // Reference to the Volume in the scene

    [Tooltip("Duration (seconds) to keep the effect at 'targetBloomIntensity' before reverting.")]
    public float effectDuration = 2f;

    [Tooltip("Bloom intensity to transition to.")]
    public float targetBloomIntensity = 5f;

    private Bloom bloomOverride;
    private float originalBloomIntensity;
    private bool hasBloom;

    protected override IEnumerator PlayEffectLogic()
    {
        if (volume == null)
        {
            Debug.LogWarning($"{name}: No Volume assigned for URP post-processing effect.");
            yield break;
        }

        // Try to grab Bloom override from the Volume
        if (volume.profile.TryGet(out bloomOverride))
        {
            hasBloom = true;
            // Store original intensity
            originalBloomIntensity = bloomOverride.intensity.value;
        }
        else
        {
            Debug.LogWarning($"{name}: No Bloom override found on Volume.");
            yield break;
        }

        // Change Bloom intensity
        bloomOverride.intensity.value = targetBloomIntensity;

        // Wait for effect duration
        yield return new WaitForSeconds(effectDuration);

        // Revert to the original intensity
        if (hasBloom)
        {
            bloomOverride.intensity.value = originalBloomIntensity;
        }
    }
}
