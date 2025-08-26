using JG.GameContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

internal static class AssetResolver
{
    // Prefix that switches the resolver into Resources.Load() mode.
    private const string RESOURCES_PREFIX = "Resources:";

    // Register per-type loaders for file mode (loose files in mod folder).
    // Extend as needed (e.g., TextAsset, Mesh).
    private static readonly Dictionary<Type, Func<string, UnityEngine.Object>> _fileLoaders =
        new()
        {
            [typeof(Texture2D)] = LoadTextureFromFile,
            [typeof(Sprite)] = p => MakeSprite(LoadTextureFromFile(p)),
            // NOTE: Provide your own WAV/OGG loader if you plan to load audio from disk.
            [typeof(AudioClip)] = LoadAudioClipFromFile
        };

    /// Injects assets into fields/properties marked with [AssetFromFile] on a definition.
    /// - File mode: loads from <modRoot>/<subFolder>/<fileName><ext>
    /// - Resources mode: loads via Resources.Load("<subPath>/<fileName>", memberType)
    public static void InjectAssets(IContentDef def, string modRoot, string modId)
    {
        if (def == null) throw new ArgumentNullException(nameof(def));
        if (modRoot == null) modRoot = string.Empty;

        var t = def.GetType();
        var members = t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var mem in members)
        {
            var a = mem.GetCustomAttribute<AssetFromFileAttribute>();
            if (a == null) continue;

            var memberType = GetMemberType(mem);
            if (memberType == null || !typeof(UnityEngine.Object).IsAssignableFrom(memberType))
            {
                Debug.LogError($"[{modId}] {t.Name}.{mem.Name} has [AssetFromFile] but type is not a UnityEngine.Object.");
                continue;
            }

            // Resolve "file name" (or logical name) from FileNameKey or fall back to Id
            var name = ResolveName(def, t, a.FileNameKey, modId, $"{t.Name}.{mem.Name}", a.Optional);
            if (string.IsNullOrWhiteSpace(name)) continue;

            // Branch by scheme
            if (IsResourcesMode(a.SubFolder))
            {
                // -------- Resources mode --------
                var subPath = a.SubFolder.Substring(RESOURCES_PREFIX.Length).Trim().TrimStart('/', '\\');
                var resourcesPath = BuildResourcesPath(subPath, name);

                try
                {
                    var asset = Resources.Load(resourcesPath, memberType);
                    if (!asset)
                    {
                        if (!a.Optional)
                            Debug.LogWarning($"[{modId}] Resources asset not found: Resources/{resourcesPath} (type {memberType.Name})");
                        continue;
                    }

                    SetMemberValue(def, mem, asset);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{modId}] Failed to load Resources {memberType.Name} for {t.Name}.{mem.Name} at '{resourcesPath}':\n{ex}");
                }
            }
            else
            {
                // -------- File mode --------
                if (!_fileLoaders.TryGetValue(memberType, out var loader))
                {
                    Debug.LogError($"[{modId}] No file loader registered for {memberType.Name}. Add one to AssetResolver._fileLoaders.");
                    continue;
                }

                var ext = a.Extension ?? DefaultExt(memberType);
                if (string.IsNullOrEmpty(ext))
                {
                    Debug.LogError($"[{modId}] Missing file extension for {memberType.Name} on {t.Name}.{mem.Name}. Specify one on [AssetFromFile] or add DefaultExt.");
                    continue;
                }

                var sub = (a.SubFolder ?? string.Empty)
                          .Replace('/', Path.DirectorySeparatorChar)
                          .Replace('\\', Path.DirectorySeparatorChar);

                var abs = Path.Combine(modRoot ?? string.Empty, sub, name + ext);

                if (!File.Exists(abs))
                {
                    if (!a.Optional)
                        Debug.LogWarning($"[{modId}] Asset file not found: {abs}");
                    continue;
                }

                try
                {
                    var asset = loader(abs);
                    if (asset == null && !a.Optional)
                    {
                        Debug.LogWarning($"[{modId}] Loader returned null for {abs} ({memberType.Name}).");
                        continue;
                    }

                    SetMemberValue(def, mem, asset);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{modId}] Failed to load {memberType.Name} for {t.Name}.{mem.Name} from '{abs}':\n{ex}");
                }
            }
        }
    }

    // ------------ Helpers ------------

    private static bool IsResourcesMode(string subFolder)
        => !string.IsNullOrEmpty(subFolder) && subFolder.StartsWith(RESOURCES_PREFIX, StringComparison.OrdinalIgnoreCase);

    private static string BuildResourcesPath(string subPath, string name)
    {
        // Allow name to include subfolders. Strip known extensions if author provided any.
        string strip(string s)
        {
            s = s.Replace('\\', '/');
            if (s.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            else if (s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            else if (s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            else if (s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            else if (s.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            else if (s.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) s = Path.ChangeExtension(s, null);
            return s.Trim('/');
        }

        subPath = strip(subPath ?? string.Empty);
        name = strip(name ?? string.Empty);

        return string.IsNullOrEmpty(subPath) ? name : $"{subPath}/{name}";
    }

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

    private static string DefaultExt(Type t)
    {
        if (t == typeof(Texture2D) || t == typeof(Sprite)) return ".png";
        if (t == typeof(AudioClip)) return ".wav";
        return string.Empty;
    }

    // ------------ File loaders (mod folder) ------------

    private static Texture2D LoadTextureFromFile(string path)
    {
        var data = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(data, markNonReadable: false))
            throw new Exception($"Failed to decode image: {path}");
        tex.name = Path.GetFileNameWithoutExtension(path);
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex)
    {
        if (!tex) throw new ArgumentNullException(nameof(tex));
        return Sprite.Create(tex,
                             new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f),
                             100f); // pixels-per-unit default
    }

    private static AudioClip LoadAudioClipFromFile(string path)
    {
        // Placeholder – implement your own WAV/OGG decoder or integrate an existing one.
        // Throwing here keeps behavior explicit if someone tries to load audio via file mode.
        throw new NotSupportedException($"Audio file loading is not implemented. Tried to load: {path}");
    }
}
