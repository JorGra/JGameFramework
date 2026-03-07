using JG.GameContent;
using JG.GameContent.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

internal static class AssetResolver
{
    private const string RESOURCES_PREFIX = "Resources:";
    private static readonly ReferenceEqualityComparer ReferenceComparer = new();

    public static void InjectAssets(IContentDef def, string modRoot, string modId, IDiagnosticSink sink = null)
    {
        if (def == null) throw new ArgumentNullException(nameof(def));
        if (modRoot == null) modRoot = string.Empty;

        var visited = new HashSet<object>(ReferenceComparer);
        ProcessObject(def, def.GetType(), def.SourceFile, modRoot, modId ?? string.Empty, visited, sink);
    }

    private static void ProcessObject(
        object target,
        Type targetType,
        string sourceFile,
        string modRoot,
        string modId,
        HashSet<object> visited,
        IDiagnosticSink sink)
    {
        if (target == null || targetType == null)
            return;

        if (target is UnityEngine.Object && target is not IContentDef)
            return;

        if (!visited.Add(target))
            return;

        var members = targetType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var mem in members)
        {
            if (mem is not FieldInfo && mem is not PropertyInfo)
                continue;

            var memberType = GetMemberType(mem);
            if (memberType == null)
                continue;

            if (typeof(UnityEngine.Object).IsAssignableFrom(memberType) &&
                mem.GetCustomAttribute<AssetFromPathAttribute>() is AssetFromPathAttribute attrPath)
            {
                ResolveFromPathAttribute(target, targetType, mem, memberType, attrPath, modRoot, modId, sourceFile, sink);
                continue;
            }

            if (mem.GetCustomAttribute<AssetsFromDirectoryAttribute>() is AssetsFromDirectoryAttribute directoryAttr)
            {
                ResolveFromDirectoryAttribute(target, targetType, mem, memberType, directoryAttr, modRoot, modId, sourceFile, sink);
                continue;
            }

            var memberValue = GetMemberValue(target, mem);
            if (memberValue == null)
                continue;

            if (memberValue is UnityEngine.Object || memberValue is string)
                continue;

            if (memberValue is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null || item is UnityEngine.Object || item is string)
                        continue;

                    var itemType = item.GetType();
                    if (!ShouldRecurseInto(itemType))
                        continue;

                    var childSource = sourceFile;
                    if (item is IContentDef childDef && !string.IsNullOrWhiteSpace(childDef.SourceFile))
                        childSource = childDef.SourceFile;

                    ProcessObject(item, itemType, childSource, modRoot, modId, visited, sink);
                }
            }
            else if (ShouldRecurseInto(memberType))
            {
                var childSource = sourceFile;
                if (memberValue is IContentDef childDef && !string.IsNullOrWhiteSpace(childDef.SourceFile))
                    childSource = childDef.SourceFile;

                ProcessObject(memberValue, memberType, childSource, modRoot, modId, visited, sink);
            }
        }
    }

    private static bool ShouldRecurseInto(Type type)
    {
        if (type == null) return false;
        if (type.IsPrimitive || type.IsEnum) return false;
        if (type == typeof(string)) return false;
        if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return false;
        if (typeof(Newtonsoft.Json.Linq.JToken).IsAssignableFrom(type)) return false;
        if (type.IsValueType) return false;
        return true;
    }

    private static bool IsResourcesKey(string key)
        => !string.IsNullOrEmpty(key) && key.StartsWith(RESOURCES_PREFIX, StringComparison.OrdinalIgnoreCase);

    private static Type GetMemberType(MemberInfo mem) =>
        mem switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => null
        };

    private static object GetMemberValue(object inst, MemberInfo mem) =>
        mem switch
        {
            FieldInfo f => f.GetValue(inst),
            PropertyInfo p when p.CanRead && p.GetIndexParameters().Length == 0 => GetPropertyValueSafe(p, inst),
            _ => null
        };

    private static object GetPropertyValueSafe(PropertyInfo property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static void SetMemberValue(object inst, MemberInfo mem, object value)
    {
        switch (mem)
        {
            case FieldInfo f:
                f.SetValue(inst, value);
                break;
            case PropertyInfo p when p.CanWrite && p.GetIndexParameters().Length == 0:
                p.SetValue(inst, value);
                break;
            case PropertyInfo p:
                Debug.LogWarning($"Property {inst.GetType().Name}.{p.Name} is read-only; skipping value set.");
                break;
        }
    }

    private static MemberInfo FindMember(Type type, string memberName)
    {
        if (type == null || string.IsNullOrWhiteSpace(memberName))
            return null;

        return type.GetMember(memberName,
                              BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   .FirstOrDefault();
    }

    private static string ResolveName(object target,
                                      Type targetType,
                                      string keyName,
                                      string modId,
                                      string context,
                                      bool optional)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            if (target is IContentDef def)
                return def.Id;
            return null;
        }

        var keyMember = FindMember(targetType, keyName);
        if (keyMember == null)
        {
            Debug.LogError($"[{modId}] FileNameKey \"{keyName}\" not found on {targetType.Name}.");
            return null;
        }

        string val = keyMember switch
        {
            FieldInfo f => f.GetValue(target) as string,
            PropertyInfo p => p.GetValue(target) as string,
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

    private static void ResolveFromPathAttribute(
        object target,
        Type targetType,
        MemberInfo mem,
        Type memberType,
        AssetFromPathAttribute attr,
        string modRoot,
        string modId,
        string sourceFile,
        IDiagnosticSink sink = null)
    {
        var key = ResolveName(target, targetType, attr.PathKey, modId, $"{targetType.Name}.{mem.Name}", attr.Optional);
        if (string.IsNullOrWhiteSpace(key)) return;

        var raw = key.Trim();
        var isResources = IsResourcesKey(raw);
        var ext = Path.GetExtension(raw);
        var fieldPath = $"{targetType.Name}.{mem.Name}";

        if (isResources)
        {
            var resPath = raw.Substring(RESOURCES_PREFIX.Length).Trim().TrimStart('/', '\\').Replace('\\', '/');
            var resPathNoExt = Path.ChangeExtension(resPath, null);

            if (!string.IsNullOrEmpty(ext) && JG.GameContent.AssetResolving.AssetResolverRegistry.TryGetByExtension(ext, out var plug))
            {
                TryLoadWithPlugin(() => plug.LoadFromResources(resPathNoExt, memberType), target, mem, memberType, modId, raw, attr.Optional, sink, sourceFile, fieldPath);
            }
            else
            {
                try
                {
                    var asset = Resources.Load(resPathNoExt, memberType);
                    if (!asset)
                    {
                        if (!attr.Optional)
                        {
                            Debug.LogWarning($"[{modId}] Resources asset not found: Resources/{resPathNoExt} (type {memberType.Name})");
                            sink?.Report(new ContentDiagnostic
                            {
                                Severity = DiagnosticSeverity.Warning,
                                Category = DiagnosticCategory.AssetResolution,
                                ModId = modId,
                                FilePath = sourceFile,
                                FieldPath = fieldPath,
                                Message = $"Resources asset not found: Resources/{resPathNoExt} (type {memberType.Name})"
                            });
                        }
                        return;
                    }
                    SetMemberValue(target, mem, asset);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{modId}] Failed to load Resources {memberType.Name} for {fieldPath} at '{resPathNoExt}':\n{ex}");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.AssetResolution,
                        ModId = modId,
                        FilePath = sourceFile,
                        FieldPath = fieldPath,
                        Message = $"Failed to load Resources {memberType.Name} at '{resPathNoExt}': {ex.Message}"
                    });
                }
            }
        }
        else
        {
            var contentRoot = !string.IsNullOrWhiteSpace(sourceFile)
                ? Path.GetDirectoryName(sourceFile)
                : null;

            var rel = raw.TrimStart('/', '\\');
            var abs = Path.Combine(contentRoot ?? modRoot ?? string.Empty,
                                   rel.Replace('/', Path.DirectorySeparatorChar)
                                      .Replace('\\', Path.DirectorySeparatorChar));

            if (!File.Exists(abs))
            {
                if (!attr.Optional)
                {
                    Debug.LogWarning($"[{modId}] Asset file not found: {abs}");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Category = DiagnosticCategory.AssetResolution,
                        ModId = modId,
                        FilePath = sourceFile,
                        FieldPath = fieldPath,
                        Message = $"Asset file not found: {abs}",
                        ActualValue = raw
                    });
                }
                return;
            }

            if (!JG.GameContent.AssetResolving.AssetResolverRegistry.TryGetByExtension(ext, out var plug))
            {
                Debug.LogError($"[{modId}] No resolver registered for extension '{ext}' referenced by {fieldPath} → '{raw}'.");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.AssetResolution,
                    ModId = modId,
                    FilePath = sourceFile,
                    FieldPath = fieldPath,
                    Message = $"No resolver registered for extension '{ext}'.",
                    ActualValue = raw
                });
                return;
            }

            TryLoadWithPlugin(() => plug.LoadFromFile(abs, memberType), target, mem, memberType, modId, abs, attr.Optional, sink, sourceFile, fieldPath);
        }
    }

    private static void TryLoadWithPlugin(Func<UnityEngine.Object> load,
                                          object target,
                                          MemberInfo mem,
                                          Type memberType,
                                          string modId,
                                          string where,
                                          bool optional,
                                          IDiagnosticSink sink = null,
                                          string sourceFile = null,
                                          string fieldPath = null)
    {
        try
        {
            var asset = load();
            if (!asset)
            {
                if (!optional)
                {
                    Debug.LogWarning($"[{modId}] Resolver returned null for {where} (expected {memberType.Name}).");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Category = DiagnosticCategory.AssetResolution,
                        ModId = modId,
                        FilePath = sourceFile,
                        FieldPath = fieldPath,
                        Message = $"Resolver returned null for '{where}' (expected {memberType.Name})."
                    });
                }
                return;
            }
            SetMemberValue(target, mem, asset);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{modId}] Failed to load {memberType.Name} from '{where}':\n{ex}");
            sink?.Report(new ContentDiagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.AssetResolution,
                ModId = modId,
                FilePath = sourceFile,
                FieldPath = fieldPath,
                Message = $"Failed to load {memberType.Name} from '{where}': {ex.Message}"
            });
        }
    }

    private static void ResolveFromDirectoryAttribute(
        object target,
        Type targetType,
        MemberInfo mem,
        Type memberType,
        AssetsFromDirectoryAttribute attr,
        string modRoot,
        string modId,
        string sourceFile,
        IDiagnosticSink sink = null)
    {
        var context = $"{targetType.Name}.{mem.Name}";

        if (!TryGetCollectionElementType(memberType, out var elementType, out var isArray))
        {
            Debug.LogError($"[{modId}] {context}: Type {memberType.Name} is not a supported collection. Use arrays or IList implementations.");
            return;
        }

        if (!typeof(UnityEngine.Object).IsAssignableFrom(elementType))
        {
            Debug.LogError($"[{modId}] {context}: Element type {elementType.Name} is not a UnityEngine.Object.");
            return;
        }

        var directoryMember = FindMember(targetType, attr.DirectoryKey);
        if (directoryMember == null)
        {
            Debug.LogError($"[{modId}] {context}: Directory member '{attr.DirectoryKey}' not found on {targetType.Name}.");
            return;
        }

        var directoryValue = GetMemberValue(target, directoryMember) as string;
        if (string.IsNullOrWhiteSpace(directoryValue))
        {
            if (!attr.Optional)
                Debug.LogWarning($"[{modId}] {context}: Directory value from '{attr.DirectoryKey}' is empty.");
            AssignCollection(target, mem, memberType, elementType, isArray, Array.Empty<UnityEngine.Object>());
            return;
        }

        var relativeDirectory = directoryValue.Replace('\\', '/').Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            if (!attr.Optional)
                Debug.LogWarning($"[{modId}] {context}: Directory value from '{attr.DirectoryKey}' resolves to an empty path.");
            AssignCollection(target, mem, memberType, elementType, isArray, Array.Empty<UnityEngine.Object>());
            return;
        }

        var baseRoot = !string.IsNullOrWhiteSpace(sourceFile)
            ? Path.GetDirectoryName(sourceFile)
            : modRoot;

        if (string.IsNullOrWhiteSpace(baseRoot))
        {
            Debug.LogError($"[{modId}] {context}: Unable to resolve base content directory.");
            return;
        }

        var absoluteDirectory = Path.GetFullPath(Path.Combine(baseRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(absoluteDirectory))
        {
            if (!attr.Optional)
                Debug.LogWarning($"[{modId}] Directory '{absoluteDirectory}' not found for {context}.");
            AssignCollection(target, mem, memberType, elementType, isArray, Array.Empty<UnityEngine.Object>());
            return;
        }

        string extensionValue = null;
        if (!string.IsNullOrWhiteSpace(attr.ExtensionKey))
        {
            var extensionMember = FindMember(targetType, attr.ExtensionKey);
            if (extensionMember == null)
            {
                Debug.LogError($"[{modId}] {context}: Extension member '{attr.ExtensionKey}' not found on {targetType.Name}.");
                return;
            }

            extensionValue = GetMemberValue(target, extensionMember) as string;
        }

        var extensions = ParseExtensions(extensionValue);
        if (extensions.Count == 0)
        {
            Debug.LogWarning($"[{modId}] {context}: No valid extensions were provided via '{attr.ExtensionKey}'.");
            AssignCollection(target, mem, memberType, elementType, isArray, Array.Empty<UnityEngine.Object>());
            return;
        }

        bool includeSubdirectories = false;
        if (!string.IsNullOrWhiteSpace(attr.IncludeSubdirectoriesKey))
        {
            var includeMember = FindMember(targetType, attr.IncludeSubdirectoriesKey);
            if (includeMember != null)
            {
                includeSubdirectories = ExtractBoolValue(target, includeMember);
            }
        }

        var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory
            .EnumerateFiles(absoluteDirectory, "*", searchOption)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (files.Count == 0)
        {
            if (!attr.Optional)
                Debug.LogWarning($"[{modId}] {context}: No files with extensions {string.Join(", ", extensions)} found in '{absoluteDirectory}'.");
            AssignCollection(target, mem, memberType, elementType, isArray, Array.Empty<UnityEngine.Object>());
            return;
        }

        var loadedAssets = new List<UnityEngine.Object>();
        foreach (var file in files)
        {
            var extension = Path.GetExtension(file);
            if (!JG.GameContent.AssetResolving.AssetResolverRegistry.TryGetByExtension(extension, out var resolver))
            {
                Debug.LogWarning($"[{modId}] {context}: No resolver registered for extension '{extension}'.");
                continue;
            }

            try
            {
                var asset = resolver.LoadFromFile(file, elementType);
                asset = EnsureAssetMatchesType(asset, elementType);
                if (!asset)
                {
                    Debug.LogWarning($"[{modId}] {context}: Resolver returned incompatible asset for '{file}'.");
                    continue;
                }

                ApplyDefaultAssetName(asset, baseRoot, file);
                loadedAssets.Add(asset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{modId}] {context}: Failed to load {elementType.Name} from '{file}':\n{ex}");
            }
        }

        AssignCollection(target, mem, memberType, elementType, isArray, loadedAssets);
    }

    private static bool ExtractBoolValue(object target, MemberInfo member)
    {
        var raw = GetMemberValue(target, member);
        if (raw is bool b) return b;

        if (raw is IConvertible convertible)
        {
            try
            {
                return convertible.ToBoolean(null);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ParseExtensions(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new[] { ".png" };

        var parts = value.Split(new[] { ';', ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            var ext = part.Trim();
            if (ext.Length == 0)
                continue;

            if (!ext.StartsWith("."))
                ext = "." + ext;

            set.Add(ext.ToLowerInvariant());
        }

        if (set.Count == 0)
            set.Add(".png");

        return set.ToArray();
    }

    private static UnityEngine.Object EnsureAssetMatchesType(UnityEngine.Object asset, Type elementType)
    {
        if (!asset)
            return null;

        if (elementType.IsInstanceOfType(asset))
            return asset;

        if (elementType == typeof(Sprite) && asset is Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name;
            return sprite;
        }

        if (elementType == typeof(Texture2D) && asset is Sprite spriteAsset && spriteAsset.texture != null)
        {
            return spriteAsset.texture;
        }

        return null;
    }

    private static void ApplyDefaultAssetName(UnityEngine.Object asset, string baseRoot, string absolutePath)
    {
        if (!asset)
            return;

        var relative = BuildRelativeKey(baseRoot, absolutePath);
        switch (asset)
        {
            case Sprite sprite:
                sprite.name = relative;
                break;
            case Texture2D texture:
                texture.name = relative;
                break;
            default:
                asset.name = relative;
                break;
        }
    }

    private static string BuildRelativeKey(string baseRoot, string absoluteFile)
    {
        try
        {
            var relative = Path.GetRelativePath(baseRoot, absoluteFile);
            return relative.Replace('\\', '/');
        }
        catch
        {
            return Path.GetFileName(absoluteFile);
        }
    }

    private static void AssignCollection(object target,
                                         MemberInfo mem,
                                         Type memberType,
                                         Type elementType,
                                         bool isArray,
                                         IReadOnlyList<UnityEngine.Object> assets)
    {
        if (isArray)
        {
            var array = Array.CreateInstance(elementType, assets.Count);
            for (int i = 0; i < assets.Count; i++)
                array.SetValue(assets[i], i);
            SetMemberValue(target, mem, array);
            return;
        }

        var current = GetMemberValue(target, mem) as IList;
        var requiresSet = false;

        if (current == null || current.IsFixedSize || current.IsReadOnly || !memberType.IsInstanceOfType(current))
        {
            var concreteType = GetConcreteListType(memberType, elementType);
            current = Activator.CreateInstance(concreteType) as IList;
            requiresSet = true;
        }
        else
        {
            current.Clear();
        }

        if (current == null)
            return;

        foreach (var asset in assets)
            current.Add(asset);

        if (requiresSet)
            SetMemberValue(target, mem, current);
    }

    private static Type GetConcreteListType(Type declaredType, Type elementType)
    {
        if (declaredType.IsInterface || declaredType.IsAbstract)
            return typeof(List<>).MakeGenericType(elementType);

        return declaredType;
    }

    private static bool TryGetCollectionElementType(Type collectionType, out Type elementType, out bool isArray)
    {
        elementType = null;
        isArray = false;

        if (collectionType.IsArray)
        {
            isArray = true;
            elementType = collectionType.GetElementType();
            return elementType != null;
        }

        if (collectionType.IsGenericType)
        {
            var args = collectionType.GetGenericArguments();
            if (args.Length == 1)
            {
                elementType = args[0];
                return true;
            }
        }

        var enumerableType = collectionType.GetInterfaces()
                                           .Concat(new[] { collectionType })
                                           .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType != null)
        {
            elementType = enumerableType.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}







