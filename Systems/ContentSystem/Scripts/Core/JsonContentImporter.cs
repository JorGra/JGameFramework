using JG.GameContent.Localization;
using JG.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JG.GameContent
{
    enum LoggingLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    /// <summary>
    /// Reads every <c>*.json</c> file in each content sub‑folder.  
    /// A file can contain <b>either</b> a single object <b>or</b> an array of objects:
    ///
    /// <code>[ { …item A… }, { …item B… } ]</code>
    /// </summary>
    public sealed class JsonContentImporter : MonoBehaviour, IContentImporter
    {
        static LoggingLevel LoggingLevel = LoggingLevel.Info;
        private static Type[] _defTypes;
        private static bool _scanned;
        private static readonly JsonSerializer _json =
            JsonSerializer.Create(new JsonSerializerSettings
            {
                Converters = { new UnityScriptableObjectConverter(), new UnityColorJsonConverter(), new UnityVector2JsonConverter(), new UnityVector3JsonConverter() },
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                Error = (_, ctx) =>
                {
                    if (LoggingLevel >= LoggingLevel.Error)
                        Debug.LogError(
                        $"[JSON] {ctx.ErrorContext.Error}  •  Path: {ctx.ErrorContext.Path}");
                }
            });

        public static void RescanContentTypes()
        {
            _defTypes = AppDomain.CurrentDomain
                                 .GetAssemblies()
                                 .Where(a => !a.IsDynamic)
                                 .SelectMany(GetTypesSafe)
                                 .Where(IsValidContentDef)
                                 .Distinct()
                                 .ToArray();
            _scanned = true;
            Debug.Log($"[JsonContentImporter] Rescanned content types: found {_defTypes.Length} def types.");
        }

        private static void EnsureScanned()
        {
            if (!_scanned) RescanContentTypes();
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static bool IsValidContentDef(Type type) =>
            type != null &&
            !type.IsAbstract &&
            typeof(IContentDef).IsAssignableFrom(type) &&
            type.GetCustomAttribute<ContentFolderAttribute>() != null;

        public void Import(IModHandle h)
        {
            EnsureScanned();
            var modId = Path.GetFileName(h.Path);

            if (LoggingLevel >= LoggingLevel.Info)
                Debug.Log($"[{modId}] Importing content definitions from {h.Path}");

            // Two-pass import: regular files first, then patch files
            foreach (var defType in _defTypes)
            {
                var folderAttr = defType.GetCustomAttribute<ContentFolderAttribute>();
                var folderPath = Path.Combine(h.Path, folderAttr.FolderName);
                if (!Directory.Exists(folderPath)) continue;

                if (LoggingLevel >= LoggingLevel.Debug)
                    Debug.Log($"[{modId}] Importing {defType.Name} definitions from {folderPath}");

                // Pass 1: regular definition files
                foreach (var fp in Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    if (!IsPatchFile(fp))
                        TryImportFile(fp, h, defType);
                }

                // Pass 2: patch files
                foreach (var fp in Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    if (IsPatchFile(fp))
                        ApplyPatchFile(fp, h, modId);
                }
            }

            // Load sidecar translation files from Translations/*.json
            ModTranslationLoader.Instance.LoadFromMod(h.Path);
        }

        public static bool IsPatchFile(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            return name.EndsWith("_patches", StringComparison.OrdinalIgnoreCase);
        }

        // --------------------------------------------------------------------

        private static void TryImportFile(string filePath, IModHandle h, Type defType)
        {
            var modId = Path.GetFileName(h.Path);          // <-- use the mod id, not the file name
            try
            {
                if (LoggingLevel >= LoggingLevel.Debug)
                    Debug.Log($"[{modId}] ▶ Importing {defType.Name}: {filePath}");

                using var sr = new StreamReader(filePath);
                using var jr = new JsonTextReader(sr) { CloseInput = false };
                var token = JToken.ReadFrom(jr);

                if (token.Type == JTokenType.Array)
                {
                    foreach (var element in token)
                        DeserializeAndRegister(element, defType, h, filePath, modId);
                }
                else
                {
                    DeserializeAndRegister(token, defType, h, filePath, modId);
                }
            }
            catch (JsonReaderException ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Malformed JSON in {filePath}  "
                             + $"(line {ex.LineNumber}, pos {ex.LinePosition}): {ex.Message}");
            }
            catch (JsonSerializationException ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Serialization error in {filePath}  "
                             + $"(path \"{ex.Path}\"): {ex.Message}");
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Unexpected failure while parsing {filePath}:\n{ex}");
            }
        }

        private static void DeserializeAndRegister(
    JToken token, Type defType, IModHandle h, string filePath, string modId)
        {
            IContentDef def;
            try
            {
                def = (IContentDef)token.ToObject(defType, _json);
                def.SourceFile = filePath; // Set the source file for the def
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Failed to deserialize {defType.Name} "
                             + $"in {filePath}: {ex.Message}");
                return;
            }

            if (def == null || string.IsNullOrWhiteSpace(def.Id))
            {
                if (LoggingLevel >= LoggingLevel.Warning)
                    Debug.LogWarning($"[{modId}] ⚠ Ignored entry in {filePath} – missing \"Id\".");
                return;
            }

            // Check for duplicates before you replace
            if (ContentCatalogue.Instance.TryGet<IContentDef>(def.Id, out var existing))
            {
                if (LoggingLevel >= LoggingLevel.Warning)
                    Debug.LogWarning($"[{modId}] ⚠ Duplicate Id \"{def.Id}\" in {filePath}. "
                               + $"Replacing definition that came from {existing.Id ?? "unknown source"}");
            }

            // Inject assets like sprites, audio clips, etc. from annotated fields
            AssetResolver.InjectAssets(def, h.Path, modId);

            ContentCatalogue.Instance.AddOrReplace(def);
            ContentCatalogue.Instance.StoreRawToken(def.Id, token, defType);

            if (LoggingLevel >= LoggingLevel.Info)
                Debug.Log($"[{modId}] ✔ Registered {defType.Name} \"{def.Id}\" from {Path.GetFileName(filePath)}");
        }

        // ---- Patch file support ----

        private static void ApplyPatchFile(string filePath, IModHandle h, string modId)
        {
            try
            {
                if (LoggingLevel >= LoggingLevel.Debug)
                    Debug.Log($"[{modId}] ▶ Applying patches from: {filePath}");

                using var sr = new StreamReader(filePath);
                using var jr = new JsonTextReader(sr) { CloseInput = false };
                var token = JToken.ReadFrom(jr);

                JArray patchArray;
                if (token.Type == JTokenType.Array)
                    patchArray = (JArray)token;
                else
                    patchArray = new JArray(token);

                foreach (var element in patchArray)
                {
                    if (element is not JObject patchObj) continue;

                    var targetId = patchObj["$patch"]?.ToString();
                    if (string.IsNullOrWhiteSpace(targetId))
                    {
                        if (LoggingLevel >= LoggingLevel.Warning)
                            Debug.LogWarning($"[{modId}] ⚠ Patch entry in {filePath} missing \"$patch\" target ID.");
                        continue;
                    }

                    var opsToken = patchObj["ops"] as JArray;
                    if (opsToken == null || opsToken.Count == 0)
                    {
                        if (LoggingLevel >= LoggingLevel.Warning)
                            Debug.LogWarning($"[{modId}] ⚠ Patch for \"{targetId}\" in {filePath} has no ops.");
                        continue;
                    }

                    var patch = new ContentPatch
                    {
                        TargetId = targetId,
                        SourceMod = modId,
                        SourceFile = filePath,
                        Ops = new List<PatchOperation>()
                    };

                    foreach (var opToken in opsToken)
                    {
                        if (opToken is not JObject opObj) continue;
                        patch.Ops.Add(new PatchOperation
                        {
                            Op = opObj["op"]?.ToString(),
                            Path = opObj["path"]?.ToString(),
                            Value = opObj["value"],
                            Values = opObj["values"] as JArray,
                            Index = opObj["index"]?.ToObject<int?>()
                        });
                    }

                    // Resolve target from catalogue
                    var rawToken = ContentCatalogue.Instance.GetRawToken(targetId);
                    var defType = ContentCatalogue.Instance.GetDefType(targetId);
                    if (rawToken == null || defType == null)
                    {
                        if (LoggingLevel >= LoggingLevel.Warning)
                            Debug.LogWarning(
                                $"[{modId}] ⚠ Patch target \"{targetId}\" not found in catalogue. " +
                                $"Skipping patch from {filePath}");
                        continue;
                    }

                    // Apply patch operations
                    var patched = ContentPatchApplier.Apply(rawToken, patch);

                    // Re-deserialize and re-register
                    DeserializeAndRegister(patched, defType, h, filePath, modId);

                    if (LoggingLevel >= LoggingLevel.Info)
                        Debug.Log(
                            $"[{modId}] ✔ Patched \"{targetId}\" ({patch.Ops.Count} ops) from {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Failed to apply patch file {filePath}:\n{ex}");
            }
        }

    }


    public sealed class UnityScriptableObjectConverter : JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
            => typeof(ScriptableObject).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader,
                                        System.Type objectType,
                                        object existingValue,
                                        JsonSerializer serializer)
        {
            var instance = ScriptableObject.CreateInstance(objectType);
            serializer.Populate(reader, instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer,
                                       object value,
                                       JsonSerializer serializer) =>
            serializer.Serialize(writer, value);
    }
}
