// StatRegistryProvider.cs (UPDATED)
// Bridges the generic content pipeline to the StatRegistry.
// Removes any JSON/Resources importers and builds the registry from:
//  - Content pipeline stats (StatDef : IStatDefinition)
//  - Optional baked SOs assigned on the StatRegistry
using JG.GameContent;
using JG.Modding;
using JG.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class StatRegistryProvider : Singleton<StatRegistryProvider>
{
    [SerializeField] private StatRegistry registry;

    [Header("Merge Behavior")]
    [SerializeField, Tooltip("If true, runtime content (StatDef) overrides baked SOs on duplicate keys.")]
    private bool runtimeOverridesBaked = true;

    public StatRegistry Registry => registry;

    EventSubscription<OnModLoadingFinishedEvent> modLoadingSubscription;

    protected override void Awake()
    {
        base.Awake();

        modLoadingSubscription = EventBus<OnModLoadingFinishedEvent>.Subscribe(OnContentIndexRebuilt, this);

        if (registry == null)
        {
            // Allow scene-only usage; you can also drop a SO reference in the inspector.
            registry = ScriptableObject.CreateInstance<StatRegistry>();
        }

        //RefreshFromContent();
    }

    /// <summary>
    /// Rebuilds the registry from the current content catalogue + baked SOs.
    /// Call again after mods reload or content hot-reload.
    /// </summary>
    public void RefreshFromContent()
    {
        IEnumerable<IStatDefinition> runtime = null;

        try
        {
            var all = ContentCatalogue.Instance.GetAll<StatDef>();
            runtime = all; // StatDef implements IStatDefinition
            foreach (var stat in runtime)
            {
                Debug.Log($"[StatRegistryProvider] Found stat: {stat.Key}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[StatRegistryProvider] Content catalogue not ready or failed: {ex.Message}");
        }

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
        modLoadingSubscription?.Dispose();
        modLoadingSubscription = null;
    }
}
