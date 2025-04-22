// StatRegistryProvider.cs
using JG.Tools;
using UnityEngine;

/// <summary>
/// Singleton loader that initializes the StatRegistry from a Resources JSON.
/// </summary>
public class StatRegistryProvider : Singleton<StatRegistryProvider>
{
    [Tooltip("Reference to the StatRegistry ScriptableObject.")]
    [SerializeField] private StatRegistry registry;

    [Tooltip("Path under Resources (without '.json') to StatDefinitions.json. " +
             "E.g. Resources/StatDefinitions/StatDefinitions.json → 'StatDefinitions/StatDefinitions'.")]
    [SerializeField] private string jsonResourcePath = "StatDefinitions/StatDefinitions";

    /// <summary>
    /// Expose the registry instance for runtime lookups.
    /// </summary>
    public StatRegistry Registry => registry;

    protected override void Awake()
    {
        if (registry == null)
        {
            Debug.LogError("StatRegistryProvider: No StatRegistry assigned.");
            return;
        }

        // Load the TextAsset from Resources
        var ta = Resources.Load<TextAsset>(jsonResourcePath);
        if (ta == null)
        {
            Debug.LogError($"StatRegistryProvider: Failed to load Resources/{jsonResourcePath}.json");
            return;
        }

        // Parse it into the registry
        registry.InitializeFromJsonText(ta.text);
    }

    /// <summary>
    /// Manual re‑initialization (e.g. if you replace the JSON at runtime).
    /// </summary>
    public void Init() => Awake();
}
