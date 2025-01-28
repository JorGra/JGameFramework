using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flickers one or more Renderers by toggling a color property (default "_BaseColor").
/// Works with URP Lit materials (which use _BaseColor).
/// </summary>
public class RendererFlickerEffect : EffectBase
{
    [Header("Renderer Flicker Settings")]
    [Tooltip("All Renderers to flicker. Each submesh in these renderers will be flickered.")]
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
    /// Internal data for each submesh we want to flicker.
    /// </summary>
    private class SubmeshInfo
    {
        public Renderer renderer;
        public int submeshIndex;
        public Color originalColor;
        public MaterialPropertyBlock mpb;
    }

    private List<SubmeshInfo> submeshInfos = new List<SubmeshInfo>();

    private void Awake()
    {
        // Gather all submeshes from each renderer
        foreach (Renderer rend in targetRenderers)
        {
            if (rend == null)
                continue;

            // For each submesh (each material slot)
            int submeshCount = rend.sharedMaterials.Length;
            for (int i = 0; i < submeshCount; i++)
            {
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb, i);

                // Try to get color from the property block
                bool hadProperty = mpb.HasProperty(colorProperty);
                Color origColor = Color.white;

                if (hadProperty)
                {
                    origColor = mpb.GetColor(colorProperty);
                }
                else
                {
                    // If property block didn't have the color,
                    // read from the shared material if possible.
                    Material mat = rend.sharedMaterials[i];
                    if (mat != null && mat.HasProperty(colorProperty))
                    {
                        origColor = mat.GetColor(colorProperty);
                    }
                }

                // Store it in the property block to have a "baseline" for flicker revert
                mpb.SetColor(colorProperty, origColor);
                rend.SetPropertyBlock(mpb, i);

                // Keep track of submesh data
                submeshInfos.Add(new SubmeshInfo
                {
                    renderer = rend,
                    submeshIndex = i,
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
        if (submeshInfos.Count == 0)
        {
            Debug.LogWarning($"{name}: No valid renderers assigned or no materials found. Flicker aborted.");
            yield break;
        }

        for (int i = 0; i < flickerCount; i++)
        {
            // Flicker ON
            SetAllColor(flickerColor);
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);

            // Flicker OFF
            SetAllToOriginal();
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);
        }

        // Ensure the final color is the original
        SetAllToOriginal();
    }

    /// <summary>
    /// Sets the color property on all submeshes to the given color.
    /// </summary>
    private void SetAllColor(Color color)
    {
        foreach (var sub in submeshInfos)
        {
            sub.mpb.SetColor(colorProperty, color);
            sub.renderer.SetPropertyBlock(sub.mpb, sub.submeshIndex);
        }
    }

    /// <summary>
    /// Restores the original color to all submeshes.
    /// </summary>
    private void SetAllToOriginal()
    {
        foreach (var sub in submeshInfos)
        {
            sub.mpb.SetColor(colorProperty, sub.originalColor);
            sub.renderer.SetPropertyBlock(sub.mpb, sub.submeshIndex);
        }
    }
}
