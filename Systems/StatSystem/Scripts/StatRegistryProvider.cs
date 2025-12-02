// StatRegistryProvider.cs (UPDATED)
// Bridges the generic content pipeline to the StatRegistry.
// Removes any JSON/Resources importers and builds the registry from:
//  - Optional content pipeline stats (StatDef : IStatDefinition) if the content system is present
//  - Optional baked SOs assigned on the StatRegistry
using JG.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class StatRegistryProvider : Singleton<StatRegistryProvider>
{
    [SerializeField] private StatRegistry registry;

    [Header("Merge Behavior")]
    [SerializeField, Tooltip("If true, runtime content (StatDef) overrides baked SOs on duplicate keys.")]
    private bool runtimeOverridesBaked = true;

    public StatRegistry Registry => registry;

    protected override void Awake()
    {
        base.Awake();

        if (registry == null)
        {
            // Allow scene-only usage; you can also drop a SO reference in the inspector.
            registry = ScriptableObject.CreateInstance<StatRegistry>();
        }

        // Try to prime the registry if content system is present
        RefreshFromContent();
    }

    /// <summary>
    /// Rebuilds the registry from the current content catalogue + baked SOs.
    /// Call again after mods reload or content hot-reload.
    /// </summary>
    public void RefreshFromContent()
    {
        IEnumerable<IStatDefinition> runtime = TryGetRuntimeStatsFromContent();

        registry.RebuildIndex(runtime, runtimeOverridesBaked);
        Debug.Log($"[StatRegistryProvider] Stat registry built. Count = {registry.Count}");
    }

    // Optional hook for external systems to notify the provider that content was rebuilt.
    // Wire this from your content pipeline's 'OnRebuilt/OnReloaded' callback if available.
    public void OnContentIndexRebuilt()
    {
        RefreshFromContent();
    }

    /// <summary>
    /// Manual re-initialization if you want to trigger it from code or editor buttons.
    /// </summary>
    public void Init() => RefreshFromContent();

    private void OnDestroy()
    {
        // no-op
    }

    /// <summary>
    /// Attempts to pull stats from the optional content system (ContentCatalogue + StatDef).
    /// If the content system is absent, returns null and the registry uses only baked SOs.
    /// </summary>
    private static IEnumerable<IStatDefinition> TryGetRuntimeStatsFromContent()
    {
        try
        {
            var contentCatalogueType = FindType("JG.GameContent.ContentCatalogue");
            var statDefType = FindType("StatDef") ?? FindType("JG.GameContent.StatDef");
            if (contentCatalogueType == null || statDefType == null)
                return null;

            var instanceProp = contentCatalogueType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProp?.GetValue(null);
            if (instance == null)
                return null;

            var getAll = contentCatalogueType.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Instance);
            if (getAll == null || !getAll.IsGenericMethodDefinition)
                return null;

            var getAllStatDef = getAll.MakeGenericMethod(statDefType);
            var result = getAllStatDef.Invoke(instance, null) as IEnumerable;
            if (result == null)
                return null;

            return result.Cast<object>()
                         .OfType<IStatDefinition>()
                         .ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StatRegistryProvider] Content system integration failed: {ex.Message}");
            return null;
        }
    }

    private static Type FindType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, throwOnError: false);
            if (t != null)
                return t;
            // Some types (like StatDef) might be in global namespace; check by name
            t = asm.GetTypes().FirstOrDefault(x => x.Name == fullName);
            if (t != null)
                return t;
        }
        return null;
    }
}
