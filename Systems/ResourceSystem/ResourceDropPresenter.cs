using System.Collections.Generic;
using JG.GameContent;
using JG.Tools;
using UnityEngine;

public class ResourceDropPresenter : Singleton<ResourceDropPresenter>
{
    public const int MaxBurst = 8;

    [Tooltip("Prefab spawned per drop. Must have a ParticleSystem + ResourceDropTracker on the root.")]
    [SerializeField] private GameObject dropPrefab;

    [Tooltip("Optional shader/material used as base for per-resource particle materials. If null, the prefab's renderer material is cloned.")]
    [SerializeField] private Material baseParticleMaterial;

    private static readonly Dictionary<int, ResourceCollector> collectorsByPlayer = new();
    private static ResourceDropPresenter cachedInstance;
    private static readonly HashSet<string> warnedMissingDef = new();
    private static bool warnedNoCollector;

    private readonly Dictionary<string, Material> materialsByResource = new();
    private readonly Queue<ResourceDropTracker> pool = new();
    private Transform poolContainer;
    private EventSubscription<ResourceDropEvent> dropSubscription;

    public static void Register(ResourceCollector collector)
    {
        if (collector == null) return;
        collectorsByPlayer[collector.PlayerId] = collector;
    }

    public static void Unregister(ResourceCollector collector)
    {
        if (collector == null) return;
        if (collectorsByPlayer.TryGetValue(collector.PlayerId, out var current) && current == collector)
        {
            collectorsByPlayer.Remove(collector.PlayerId);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        cachedInstance = this;
        if (poolContainer == null)
        {
            var go = new GameObject("[POOL] ResourceDrops");
            poolContainer = go.transform;
        }
    }

    private void OnEnable()
    {
        dropSubscription = this.SubscribeEvent<ResourceDropEvent>(OnDrop);
    }

    private void OnDisable()
    {
        dropSubscription?.Dispose();
        dropSubscription = null;
    }

    private void OnDrop(ResourceDropEvent e)
    {
        if (string.IsNullOrEmpty(e.ResourceId) || e.Amount <= 0) return;

        if (!ContentCatalogue.Instance.TryGet<ResourceDef>(e.ResourceId, out var def))
        {
            if (warnedMissingDef.Add(e.ResourceId))
            {
                Debug.LogWarning($"[ResourceDrop] No ResourceDef '{e.ResourceId}'. Crediting silently.");
            }
            CreditFallback(e);
            return;
        }

        if (e.PlayerId.HasValue)
        {
            if (collectorsByPlayer.TryGetValue(e.PlayerId.Value, out var collector) && collector.Accepts(e.ResourceId))
            {
                SpawnDrop(def, collector, e);
            }
            else
            {
                ResourceManager.Instance.AddResource(e.PlayerId.Value, e.ResourceId, e.Amount);
            }
            return;
        }

        if (collectorsByPlayer.Count == 0)
        {
            if (!warnedNoCollector)
            {
                warnedNoCollector = true;
                Debug.LogWarning("[ResourceDrop] Broadcast drop with no collectors registered. Falling back to AddResourceToAll.");
            }
            ResourceManager.Instance.AddResourceToAll(e.ResourceId, e.Amount);
            return;
        }

        foreach (var kv in collectorsByPlayer)
        {
            var collector = kv.Value;
            if (!collector.Accepts(e.ResourceId)) continue;
            SpawnDrop(def, collector, e);
        }
    }

    private void CreditFallback(ResourceDropEvent e)
    {
        if (e.PlayerId.HasValue)
        {
            ResourceManager.Instance.AddResource(e.PlayerId.Value, e.ResourceId, e.Amount);
        }
        else
        {
            ResourceManager.Instance.AddResourceToAll(e.ResourceId, e.Amount);
        }
    }

    private void SpawnDrop(ResourceDef def, ResourceCollector collector, ResourceDropEvent e)
    {
        if (dropPrefab == null)
        {
            Debug.LogWarning("[ResourceDrop] No drop prefab assigned. Crediting silently.");
            ResourceManager.Instance.AddResource(collector.PlayerId, e.ResourceId, e.Amount);
            return;
        }

        var tracker = AcquireTracker();
        if (tracker == null)
        {
            ResourceManager.Instance.AddResource(collector.PlayerId, e.ResourceId, e.Amount);
            return;
        }

        int burst = Mathf.Clamp(e.Amount, 1, MaxBurst);
        int unit = e.Amount / burst;
        int remainder = e.Amount - unit * burst;

        var mat = GetOrCreateMaterial(def, tracker);
        tracker.Spawn(this, collector.PlayerId, e.ResourceId, burst, unit, remainder, e.WorldPosition, mat, collector.Attractor, def.collectSound);
    }

    private ResourceDropTracker AcquireTracker()
    {
        while (pool.Count > 0)
        {
            var t = pool.Dequeue();
            if (t != null) return t;
        }

        var go = Instantiate(dropPrefab, poolContainer);
        var tracker = go.GetComponent<ResourceDropTracker>();
        if (tracker == null)
        {
            Debug.LogError("[ResourceDrop] dropPrefab is missing ResourceDropTracker.");
            Destroy(go);
            return null;
        }
        return tracker;
    }

    public void Return(ResourceDropTracker tracker)
    {
        if (tracker == null) return;
        tracker.gameObject.SetActive(false);
        tracker.transform.SetParent(poolContainer, false);
        pool.Enqueue(tracker);
    }

    private Material GetOrCreateMaterial(ResourceDef def, ResourceDropTracker tracker)
    {
        if (def == null || def.icon == null) return null;
        if (materialsByResource.TryGetValue(def.Id, out var cached) && cached != null) return cached;

        var src = baseParticleMaterial;
        if (src == null)
        {
            var renderer = tracker.GetComponent<ParticleSystemRenderer>();
            src = renderer != null ? renderer.sharedMaterial : null;
        }
        if (src == null) return null;

        var mat = new Material(src) { name = $"ResourceDrop_{def.Id}" };
        mat.mainTexture = def.icon.texture;
        materialsByResource[def.Id] = mat;
        return mat;
    }
}
