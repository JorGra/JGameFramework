using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flickers one or more Renderers by toggling a color property (default "_BaseColor").
/// Works with URP Lit materials (which use _BaseColor).
/// Adjusted so that *all* materials on each renderer are updated.
/// </summary>
public class RendererFlickerEffect : EffectBase
{
    [Header("Renderer Flicker Settings")]
    [Tooltip("All Renderers to flicker. Each material in these renderers will be flickered.")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Tooltip("Property name to set color on. Default for URP Lit is \"_BaseColor\".")]
    public string colorProperty = "_BaseColor";

    [Tooltip("Number of flickers (on-off cycles).")]
    public int flickerCount = 5;

    [Tooltip("Color used during flicker.")]
    public Color flickerColor = Color.red;

    [Tooltip("Total time for each flicker cycle (on + off).")]
    public float flickerCycleTime = 0.2f;

    /// <summary>
    /// Tracks information for each material slot we'll flicker.
    /// </summary>
    private class MaterialInfo
    {
        public Renderer renderer;
        public int materialIndex;
        public Color originalColor;
        public MaterialPropertyBlock mpb;
    }

    private readonly List<MaterialInfo> materialInfos = new List<MaterialInfo>();

    public override void InitEffector()
    {
        // Gather all materials from each renderer
        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null) continue;

            int materialCount = rend.sharedMaterials.Length;
            for (int i = 0; i < materialCount; i++)
            {
                // Create a fresh property block for this material slot
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb, i);

                // Try to get the original color from the property block or fallback to the shared material
                Color origColor = Color.white;
                if (mpb.HasProperty(colorProperty))
                {
                    origColor = mpb.GetColor(colorProperty);
                }
                else
                {
                    Material mat = rend.sharedMaterials[i];
                    if (mat != null && mat.HasProperty(colorProperty))
                    {
                        origColor = mat.GetColor(colorProperty);
                    }
                }

                // Update the property block to store our baseline color
                mpb.SetColor(colorProperty, origColor);
                rend.SetPropertyBlock(mpb, i);

                // Keep track of this material slot
                materialInfos.Add(new MaterialInfo
                {
                    renderer = rend,
                    materialIndex = i,
                    originalColor = origColor,
                    mpb = mpb
                });
            }
        }
    }

    /// <summary>
    /// Main flicker logic. Each cycle toggles between flickerColor and the original color.
    /// </summary>
    protected override IEnumerator PlayEffectLogic()
    {
        if (materialInfos.Count == 0)
        {
            Debug.LogWarning($"{name}: No valid materials found. Flicker aborted.");
            yield break;
        }

        for (int i = 0; i < flickerCount; i++)
        {
            // Flicker ON
            SetAllColor(flickerColor);
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);

            // Flicker OFF
            SetAllOriginal();
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);
        }

        // Ensure the final color is the original
        SetAllOriginal();
    }

    /// <summary>
    /// Sets the color property on all tracked material slots.
    /// </summary>
    private void SetAllColor(Color color)
    {
        foreach (var matInfo in materialInfos)
        {
            matInfo.mpb.SetColor(colorProperty, color);
            matInfo.renderer.SetPropertyBlock(matInfo.mpb, matInfo.materialIndex);
        }
    }

    /// <summary>
    /// Restores the original color on all tracked material slots.
    /// </summary>
    private void SetAllOriginal()
    {
        foreach (var matInfo in materialInfos)
        {
            matInfo.mpb.SetColor(colorProperty, matInfo.originalColor);
            matInfo.renderer.SetPropertyBlock(matInfo.mpb, matInfo.materialIndex);
        }
    }
}
