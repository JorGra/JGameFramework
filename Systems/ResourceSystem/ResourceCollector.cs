using System.Reflection;
using UnityEngine;

public class ResourceCollector : MonoBehaviour
{
    [Tooltip("If >= 0, forces the player id. If < 0, the collector reads PlayerIndex from a parent component (any class with an int 'PlayerIndex' member) and re-registers when it changes.")]
    [SerializeField] private int playerIdOverride = -1;

    [Tooltip("WorldParticleAttractor that pulls drop particles toward this collector. Place on a child of the player sprite. Auto-binds to a component on the same GameObject if left empty.")]
    public WorldParticleAttractor attractor;

    [Tooltip("If non-empty, only resources with these ids are collected by this player. Empty = collect everything.")]
    public string[] resourceFilter;

    private int currentPlayerId = int.MinValue;
    private bool registered;
    private object cachedSource;
    private MemberInfo cachedMember;

    public WorldParticleAttractor Attractor => attractor;
    public int PlayerId => currentPlayerId;

    public bool Accepts(string resourceId)
    {
        if (resourceFilter == null || resourceFilter.Length == 0) return true;
        for (int i = 0; i < resourceFilter.Length; i++)
        {
            if (resourceFilter[i] == resourceId) return true;
        }
        return false;
    }

    private void OnEnable()
    {
        if (attractor == null)
        {
            attractor = GetComponent<WorldParticleAttractor>() ?? GetComponentInChildren<WorldParticleAttractor>(true);
        }
        SyncRegistration(force: true);
    }

    private void OnDisable()
    {
        if (registered)
        {
            ResourceDropPresenter.Unregister(this);
            registered = false;
        }
    }

    private void Update()
    {
        if (playerIdOverride < 0)
        {
            SyncRegistration(force: false);
        }
    }

    private void SyncRegistration(bool force)
    {
        int resolved = ResolvePlayerId();
        if (!force && resolved == currentPlayerId && registered) return;

        if (registered)
        {
            ResourceDropPresenter.Unregister(this);
            registered = false;
        }

        currentPlayerId = resolved;
        ResourceDropPresenter.Register(this);
        registered = true;
    }

    private int ResolvePlayerId()
    {
        if (playerIdOverride >= 0) return playerIdOverride;

        if (cachedSource == null || (cachedSource is Object u && u == null))
        {
            cachedSource = null;
            cachedMember = null;
            var components = GetComponentsInParent<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (c == null) continue;
                var t = c.GetType();
                var prop = t.GetProperty("PlayerIndex", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.PropertyType == typeof(int))
                {
                    cachedSource = c;
                    cachedMember = prop;
                    break;
                }
                var field = t.GetField("PlayerIndex", BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    cachedSource = c;
                    cachedMember = field;
                    break;
                }
            }
        }

        if (cachedMember is PropertyInfo p) return (int)p.GetValue(cachedSource);
        if (cachedMember is FieldInfo f) return (int)f.GetValue(cachedSource);
        return 0;
    }
}
