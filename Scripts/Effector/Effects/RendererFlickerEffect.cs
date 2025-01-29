using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flickers one or more Renderers by toggling a base color (default "_BaseColor")
/// and, optionally, an emissive color (default "_EmissionColor").
/// Works with URP Lit materials which have "_BaseColor" and "_EmissionColor" properties.
/// </summary>
public class RendererFlickerEffect : EffectBase
{
    [Header("Renderer Flicker Settings")]
    [Tooltip("All Renderers to flicker. Each material in these renderers will be flickered.")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Tooltip("Property name to set base color on (e.g. '_BaseColor' for URP Lit).")]
    public string colorProperty = "_BaseColor";

    [Tooltip("Number of flickers (on-off cycles).")]
    public int flickerCount = 5;

    [Tooltip("Color used during flicker (for the base color).")]
    public Color flickerColor = Color.red;

    [Tooltip("Total time for each flicker cycle (on + off).")]
    public float flickerCycleTime = 0.2f;

    [Header("Emissive Flicker Settings")]
    [Tooltip("If true, we also set an emissive color on the material.")]
    public bool useEmissive = false;

    [Tooltip("Property name for emissive color (e.g. '_EmissionColor').")]
    public string emissiveProperty = "_EmissionColor";

    [Tooltip("Color used for emissive flicker, if 'useEmissive' is true.")]
    public Color emissiveColor = Color.white;

    [Tooltip("Multiplier for emissive color intensity.")]
    public float emissiveStrength = 1.0f;

    /// <summary>
    /// Tracks information for each material slot we'll flicker.
    /// </summary>
    private class MaterialInfo
    {
        public Renderer renderer;
        public int materialIndex;

        public Color originalColor;
        public Color originalEmissive;

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
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb, i);

                // Read the base color
                Color baseColor = Color.white;
                if (mpb.HasProperty(colorProperty))
                {
                    baseColor = mpb.GetColor(colorProperty);
                }
                else
                {
                    var mat = rend.sharedMaterials[i];
                    if (mat != null && mat.HasProperty(colorProperty))
                    {
                        baseColor = mat.GetColor(colorProperty);
                    }
                }

                // Read the original emissive color
                Color baseEmissive = Color.black;
                if (mpb.HasProperty(emissiveProperty))
                {
                    baseEmissive = mpb.GetColor(emissiveProperty);
                }
                else
                {
                    var mat = rend.sharedMaterials[i];
                    if (mat != null && mat.HasProperty(emissiveProperty))
                    {
                        baseEmissive = mat.GetColor(emissiveProperty);
                    }
                }

                // Update property block with these baseline values
                mpb.SetColor(colorProperty, baseColor);
                if (mpb.HasProperty(emissiveProperty))
                {
                    mpb.SetColor(emissiveProperty, baseEmissive);
                }

                rend.SetPropertyBlock(mpb, i);

                materialInfos.Add(new MaterialInfo
                {
                    renderer = rend,
                    materialIndex = i,
                    mpb = mpb,
                    originalColor = baseColor,
                    originalEmissive = baseEmissive
                });
            }
        }
    }

    /// <summary>
    /// Main flicker logic. Each cycle toggles between flickerColor/emissiveColor
    /// and the original material colors.
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
            SetAllColorsOn();
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);

            // Flicker OFF
            SetAllColorsOff();
            yield return new WaitForSeconds(flickerCycleTime * 0.5f);
        }

        // Ensure final color is the original
        SetAllColorsOff();
    }

    /// <summary>
    /// Set flicker color (and optional emissive) for all materials.
    /// </summary>
    private void SetAllColorsOn()
    {
        foreach (var matInfo in materialInfos)
        {
            // Base color
            matInfo.mpb.SetColor(colorProperty, flickerColor);

            // Optionally set emissive
            if (useEmissive && matInfo.mpb.HasProperty(emissiveProperty))
            {
                // Combine the user-chosen emissive color with the strength
                Color finalEmissive = emissiveColor * emissiveStrength;
                matInfo.mpb.SetColor(emissiveProperty, finalEmissive);
            }

            matInfo.renderer.SetPropertyBlock(matInfo.mpb, matInfo.materialIndex);
        }
    }

    /// <summary>
    /// Restore original color (and emissive) for all materials.
    /// </summary>
    private void SetAllColorsOff()
    {
        foreach (var matInfo in materialInfos)
        {
            // Restore base color
            matInfo.mpb.SetColor(colorProperty, matInfo.originalColor);

            // Restore emissive if property exists
            if (matInfo.mpb.HasProperty(emissiveProperty))
            {
                matInfo.mpb.SetColor(emissiveProperty, matInfo.originalEmissive);
            }

            matInfo.renderer.SetPropertyBlock(matInfo.mpb, matInfo.materialIndex);
        }
    }
}
