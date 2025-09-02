using JG.GameContent;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class AssetResolver
{
    // Prefix that switches the resolver into Resources.Load() mode.
    private const string RESOURCES_PREFIX = "Resources:";

    /// Injects assets into fields/properties marked with [AssetFromPath] on a definition.
    public static void InjectAssets(IContentDef def, string modRoot, string modId)
    {
        if (def == null) throw new ArgumentNullException(nameof(def));
        if (modRoot == null) modRoot = string.Empty;

        var t = def.GetType();
        var members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var mem in members)
        {
            var memberType = GetMemberType(mem);
            if (memberType == null || !typeof(UnityEngine.Object).IsAssignableFrom(memberType))
                continue; // only bind UnityEngine.Object values

            var attrPath = mem.GetCustomAttribute<AssetFromPathAttribute>();
            if (attrPath != null)
            {
                ResolveFromPathAttribute(def, mem, memberType, attrPath, modRoot, modId);
                continue;
            }

            // No legacy path: only [AssetFromPath] is supported now.
        }
    }

    // ------------ Helpers ------------

    private static bool IsResourcesKey(string key)
        => !string.IsNullOrEmpty(key) && key.StartsWith(RESOURCES_PREFIX, StringComparison.OrdinalIgnoreCase);

    // No BuildResourcesPath needed; new path mode builds directly.

    private static Type GetMemberType(MemberInfo mem) =>
        mem switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => null
        };

    private static void SetMemberValue(object inst, MemberInfo mem, UnityEngine.Object value)
    {
        switch (mem)
        {
            case FieldInfo f:
                f.SetValue(inst, value);
                break;
            case PropertyInfo p:
                if (p.CanWrite) p.SetValue(inst, value);
                else Debug.LogWarning($"Property {inst.GetType().Name}.{p.Name} is read-only; skipping value set.");
                break;
        }
    }

    private static string ResolveName(IContentDef def, Type t, string keyName, string modId, string context, bool optional)
    {
        if (string.IsNullOrEmpty(keyName))
            return def.Id;

        var keyMember = t.GetMember(keyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
        if (keyMember == null)
        {
            Debug.LogError($"[{modId}] FileNameKey \"{keyName}\" not found on {t.Name}.");
            return null;
        }

        string val = keyMember switch
        {
            FieldInfo f => f.GetValue(def) as string,
            PropertyInfo p => p.GetValue(def) as string,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(val))
        {
            if (!optional)
                Debug.LogWarning($"[{modId}] {context}: FileNameKey \"{keyName}\" is empty.");
            return null;
        }

        return val;
    }

    // Legacy file loaders removed; file loading is handled by extension plugins.

    // -------------------- New path-based resolution --------------------
    private static void ResolveFromPathAttribute(
        IContentDef def,
        MemberInfo mem,
        Type memberType,
        AssetFromPathAttribute attr,
        string modRoot,
        string modId)
    {
        var t = def.GetType();
        var key = ResolveName(def, t, attr.PathKey, modId, $"{t.Name}.{mem.Name}", attr.Optional);
        if (string.IsNullOrWhiteSpace(key)) return;

        var raw = key.Trim();
        var isResources = IsResourcesKey(raw);
        string ext = Path.GetExtension(raw);

        // Normalize paths
        if (isResources)
        {
            var resPath = raw.Substring(RESOURCES_PREFIX.Length).Trim().TrimStart('/', '\\').Replace('\\', '/');
            var resPathNoExt = Path.ChangeExtension(resPath, null);

            // If extension is known, try plugin; else fallback to direct Resources.Load by member type.
            if (!string.IsNullOrEmpty(ext) && JG.GameContent.AssetResolving.AssetResolverRegistry.TryGetByExtension(ext, out var plug))
            {
                TryLoadWithPlugin(() => plug.LoadFromResources(resPathNoExt, memberType), def, mem, memberType, modId, raw, attr.Optional);
            }
            else
            {
                try
                {
                    var asset = Resources.Load(resPathNoExt, memberType);
                    if (!asset)
                    {
                        if (!attr.Optional)
                            Debug.LogWarning($"[{modId}] Resources asset not found: Resources/{resPathNoExt} (type {memberType.Name})");
                        return;
                    }
                    SetMemberValue(def, mem, asset);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{modId}] Failed to load Resources {memberType.Name} for {t.Name}.{mem.Name} at '{resPathNoExt}':\n{ex}");
                }
            }
        }
        else
        {
            // File mode, path is relative to current content folder (directory of the JSON file)
            var contentRoot = Path.GetDirectoryName(def.SourceFile);
            var rel = raw.TrimStart('/', '\\');
            var abs = Path.Combine(contentRoot ?? modRoot ?? string.Empty,
                                   rel.Replace('/', Path.DirectorySeparatorChar)
                                      .Replace('\\', Path.DirectorySeparatorChar));

            if (!File.Exists(abs))
            {
                if (!attr.Optional)
                    Debug.LogWarning($"[{modId}] Asset file not found: {abs}");
                return;
            }

            if (!JG.GameContent.AssetResolving.AssetResolverRegistry.TryGetByExtension(ext, out var plug))
            {
                Debug.LogError($"[{modId}] No resolver registered for extension '{ext}' referenced by {t.Name}.{mem.Name} → '{raw}'.");
                return;
            }

            TryLoadWithPlugin(() => plug.LoadFromFile(abs, memberType), def, mem, memberType, modId, abs, attr.Optional);
        }
    }

    private static void TryLoadWithPlugin(Func<UnityEngine.Object> load,
                                          IContentDef def,
                                          MemberInfo mem,
                                          Type memberType,
                                          string modId,
                                          string where,
                                          bool optional)
    {
        try
        {
            var asset = load();
            if (!asset)
            {
                if (!optional)
                    Debug.LogWarning($"[{modId}] Resolver returned null for {where} (expected {memberType.Name}).");
                return;
            }
            SetMemberValue(def, mem, asset);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{modId}] Failed to load {memberType.Name} from '{where}':\n{ex}");
        }
    }

    // Legacy support removed; migrate to [AssetFromPath].
}
