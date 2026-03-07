using System;
using System.Linq;
using JG.GameContent.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.GameContent
{
    public static class ContentPatchApplier
    {
        public static JToken Apply(JToken original, ContentPatch patch, IDiagnosticSink sink = null)
        {
            var result = original.DeepClone();

            foreach (var op in patch.Ops)
            {
                try
                {
                    switch (op.Op?.ToLowerInvariant())
                    {
                        case "set":
                            ApplySet(result, op);
                            break;
                        case "remove":
                            ApplyRemove(result, op);
                            break;
                        case "add":
                            ApplyAdd(result, op);
                            break;
                        case "merge":
                            ApplyMerge(result, op);
                            break;
                        default:
                            Debug.LogWarning(
                                $"[Patch] Unknown op \"{op.Op}\" in patch for \"{patch.TargetId}\" " +
                                $"({patch.SourceFile})");
                            sink?.Report(new ContentDiagnostic
                            {
                                Severity = DiagnosticSeverity.Warning,
                                Category = DiagnosticCategory.Patch,
                                ModId = patch.SourceMod,
                                FilePath = patch.SourceFile,
                                DefId = patch.TargetId,
                                FieldPath = op.Path,
                                Message = $"Unknown patch op \"{op.Op}\".",
                                Detail = "Valid operations are: set, remove, add, merge."
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Patch] Failed to apply {op.Op} at \"{op.Path}\" for \"{patch.TargetId}\" " +
                        $"({patch.SourceFile}): {ex.Message}");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Category = DiagnosticCategory.Patch,
                        ModId = patch.SourceMod,
                        FilePath = patch.SourceFile,
                        DefId = patch.TargetId,
                        FieldPath = op.Path,
                        Message = $"Failed to apply {op.Op} at \"{op.Path}\": {ex.Message}"
                    });
                }
            }

            return result;
        }

        private static void ApplySet(JToken root, PatchOperation op)
        {
            var (parent, key) = ResolveParentAndKey(root, op.Path);
            if (parent == null)
            {
                Debug.LogWarning($"[Patch] set: could not resolve path \"{op.Path}\"");
                return;
            }

            if (parent is JObject obj)
                obj[key] = op.Value?.DeepClone();
            else if (parent is JArray arr && int.TryParse(key, out var idx) && idx >= 0 && idx < arr.Count)
                arr[idx] = op.Value?.DeepClone();
            else
                Debug.LogWarning($"[Patch] set: cannot set key \"{key}\" on {parent.Type}");
        }

        private static void ApplyRemove(JToken root, PatchOperation op)
        {
            var (parent, key) = ResolveParentAndKey(root, op.Path);
            if (parent == null)
            {
                Debug.LogWarning($"[Patch] remove: could not resolve path \"{op.Path}\"");
                return;
            }

            if (parent is JObject obj)
            {
                obj.Remove(key);
            }
            else if (parent is JArray arr)
            {
                if (int.TryParse(key, out var idx) && idx >= 0 && idx < arr.Count)
                    arr.RemoveAt(idx);
                else
                    Debug.LogWarning($"[Patch] remove: invalid array index \"{key}\"");
            }
        }

        private static void ApplyAdd(JToken root, PatchOperation op)
        {
            var target = ResolvePath(root, op.Path);
            if (target is not JArray arr)
            {
                Debug.LogWarning($"[Patch] add: path \"{op.Path}\" does not resolve to an array");
                return;
            }

            var items = op.Values ?? (op.Value != null ? new JArray(op.Value) : null);
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning($"[Patch] add: no value(s) provided for path \"{op.Path}\"");
                return;
            }

            var insertAt = op.Index ?? arr.Count;
            insertAt = Math.Clamp(insertAt, 0, arr.Count);

            for (var i = 0; i < items.Count; i++)
                arr.Insert(insertAt + i, items[i].DeepClone());
        }

        private static void ApplyMerge(JToken root, PatchOperation op)
        {
            var target = ResolvePath(root, op.Path);
            if (target is not JObject targetObj)
            {
                Debug.LogWarning($"[Patch] merge: path \"{op.Path}\" does not resolve to an object");
                return;
            }

            if (op.Value is not JObject mergeObj)
            {
                Debug.LogWarning($"[Patch] merge: value must be an object for path \"{op.Path}\"");
                return;
            }

            DeepMerge(targetObj, mergeObj);
        }

        private static void DeepMerge(JObject target, JObject source)
        {
            foreach (var prop in source.Properties())
            {
                var existing = target[prop.Name];
                if (existing is JObject existingObj && prop.Value is JObject srcObj)
                    DeepMerge(existingObj, srcObj);
                else
                    target[prop.Name] = prop.Value.DeepClone();
            }
        }

        // ---- Path resolution ----

        public static JToken ResolvePath(JToken root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;

            var segments = path.Split('/');
            var current = root;

            foreach (var seg in segments)
            {
                if (current == null)
                    return null;

                current = ResolveSegment(current, seg);
            }

            return current;
        }

        /// <summary>
        /// Resolves all segments except the last one, returning the parent token and the final key.
        /// </summary>
        private static (JToken parent, string key) ResolveParentAndKey(JToken root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return (null, null);

            var segments = path.Split('/');
            if (segments.Length == 1)
                return (root, segments[0]);

            var parentPath = string.Join("/", segments, 0, segments.Length - 1);
            var parent = ResolvePath(root, parentPath);
            var key = segments[^1];

            // If the last segment is a match expression, resolve it to an index
            if (key.StartsWith("[") && key.EndsWith("]") && key.Contains("=") && parent is JArray arr)
            {
                var matchIdx = FindMatchIndex(arr, key);
                if (matchIdx >= 0)
                    return (arr, matchIdx.ToString());
                return (null, null);
            }

            return (parent, key);
        }

        private static JToken ResolveSegment(JToken current, string segment)
        {
            // Match-based: [key=value]
            if (segment.StartsWith("[") && segment.EndsWith("]") && segment.Contains("="))
            {
                if (current is JArray arr)
                {
                    var idx = FindMatchIndex(arr, segment);
                    return idx >= 0 ? arr[idx] : null;
                }
                return null;
            }

            // Index-based for arrays
            if (current is JArray array && int.TryParse(segment, out var index))
            {
                return index >= 0 && index < array.Count ? array[index] : null;
            }

            // Property-based for objects
            if (current is JObject obj)
            {
                // Case-insensitive property lookup
                var prop = obj.Properties()
                    .FirstOrDefault(p => string.Equals(p.Name, segment, StringComparison.OrdinalIgnoreCase));
                return prop?.Value;
            }

            return null;
        }

        private static int FindMatchIndex(JArray array, string matchExpr)
        {
            // Parse "[key=value]"
            var inner = matchExpr.Substring(1, matchExpr.Length - 2);
            var eqIndex = inner.IndexOf('=');
            if (eqIndex < 0) return -1;

            var matchKey = inner.Substring(0, eqIndex);
            var matchValue = inner.Substring(eqIndex + 1);

            for (var i = 0; i < array.Count; i++)
            {
                if (array[i] is JObject item)
                {
                    var prop = item.Properties()
                        .FirstOrDefault(p => string.Equals(p.Name, matchKey, StringComparison.OrdinalIgnoreCase));
                    if (prop != null && string.Equals(prop.Value?.ToString(), matchValue, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }
    }
}
