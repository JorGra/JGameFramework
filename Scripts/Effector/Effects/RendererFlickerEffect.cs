using System.Collections;
using UnityEngine;

/// <summary>
/// Causes the assigned Renderer to flicker between two colors.
/// </summary>
public class RendererFlickerEffect : EffectBase
{
    [Header("Flicker Settings")]
    [Tooltip("Renderer to flicker.")]
    public Renderer targetRenderer;

    [Tooltip("Number of flickers.")]
    public int flickerCount = 5;

    [Tooltip("Color used during flicker.")]
    public Color flickerColor = Color.red;

    [Tooltip("Time (in seconds) for each flicker cycle (on + off).")]
    public float flickerCycleTime = 0.2f;

    private Color originalColor;
    private MaterialPropertyBlock materialPropertyBlock;

    /// <summary>
    /// We store the original color so we can revert after flickers.
    /// </summary>
    private void Awake()
    {
        if (targetRenderer != null)
        {
            // We assume the renderer uses a single material for demonstration.
            // For multiple materials, you could store multiple original colors.
            materialPropertyBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(materialPropertyBlock, 0);
            originalColor = materialPropertyBlock.GetVector("_Color");
        }
    }

    protected override IEnumerator PlayEffectLogic()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"{name}: No Renderer assigned for flicker.");
            yield break;
        }

        // If we haven't grabbed the original color yet, do so now (in case Awake didn't run).
        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(materialPropertyBlock, 0);
            originalColor = materialPropertyBlock.GetVector("_Color");
        }

        // Flicker the renderer
        for (int i = 0; i < flickerCount; i++)
        {
            // Flicker ON
            SetColor(flickerColor);
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);

            // Flicker OFF (return to original color)
            SetColor(originalColor);
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);
        }

        // Make sure we revert to the original color in case there's an odd flicker count
        SetColor(originalColor);
    }

    private void SetColor(Color color)
    {
        materialPropertyBlock.SetColor("_Color", color);
        targetRenderer.SetPropertyBlock(materialPropertyBlock, 0);
    }
}
