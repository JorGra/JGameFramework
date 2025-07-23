using JG.GameContent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using UnityEngine;

internal static class AssetResolver
{
    // Register new types once                       ▼ loaders you already have
    private static readonly Dictionary<Type, Func<string, UnityEngine.Object>> _load =
        new()
        {
            [typeof(Texture2D)] = LoadTexture,
            [typeof(Sprite)] = p => MakeSprite(LoadTexture(p)),
            [typeof(AudioClip)] = LoadAudioClip
        };

    public static void InjectAssets(IContentDef def, string modRoot, string modId)
    {
        var t = def.GetType();
        foreach (var mem in t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var a = mem.GetCustomAttribute<AssetFromFileAttribute>();
            if (a == null) continue;

            Type memType = mem switch
            {
                FieldInfo f => f.FieldType,
                PropertyInfo p => p.PropertyType,
                _ => null
            };
            if (memType == null ||
                !typeof(UnityEngine.Object).IsAssignableFrom(memType))
            {
                Debug.LogError($"[{modId}] {t.Name}.{mem.Name} has [AssetFromFile] but is not a UnityEngine.Object");
                continue;
            }

            if (!_load.TryGetValue(memType, out var loader))
            {
                Debug.LogError($"[{modId}] No loader registered for {memType.Name}");
                continue;
            }

            // -------- build file name ----------
            string fileName = def.Id;                              // default
            if (!string.IsNullOrEmpty(a.FileNameKey))
            {
                var keyMem = t.GetMember(a.FileNameKey,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                if (keyMem == null)
                {
                    Debug.LogError($"[{modId}] FileNameKey \"{a.FileNameKey}\" not found on {t.Name}");
                    continue;
                }

                fileName = keyMem switch
                {
                    FieldInfo f => f.GetValue(def) as string,
                    PropertyInfo p => p.GetValue(def) as string,
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    if (!a.Optional)
                        Debug.LogWarning($"[{modId}] {t.Name}.{mem.Name}: FileNameKey \"{a.FileNameKey}\" is empty");
                    continue;
                }
            }

            string ext = a.Extension ?? DefaultExt(memType);
            string abs = Path.Combine(modRoot,
                           a.SubFolder.Replace('/', Path.DirectorySeparatorChar),
                           fileName + ext);

            if (!File.Exists(abs))
            {
                if (!a.Optional)
                    Debug.LogWarning($"[{modId}] Asset file not found: {abs}");
                continue;
            }

            try
            {
                var asset = loader(abs);

                if (mem is FieldInfo f) f.SetValue(def, asset);
                else ((PropertyInfo)mem).SetValue(def, asset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{modId}] Failed to load {memType.Name} for {t.Name}.{mem.Name}:\n{ex}");
            }
        }
    }

    // ------------ helpers (unchanged) ----------
    private static string DefaultExt(Type t) => t == typeof(AudioClip) ? ".wav" : ".png";
    private static Texture2D LoadTexture(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) throw new Exception("Texture load failed");
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    private static Sprite MakeSprite(Texture2D tex)
    {
        return Sprite.Create(tex,
                             new Rect(0, 0, tex.width, tex.height),
                             new Vector2(0.5f, 0.5f),
                             100);
    }

    private static AudioClip LoadAudioClip(string path)
    {
        var data = File.ReadAllBytes(path);
        //var wav = WavUtility.ToAudioClip(data, Path.GetFileNameWithoutExtension(path));
        throw new NotImplementedException("WavUtility is not implemented in this context");
        //return wav;
    }
}
