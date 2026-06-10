using JG.GameContent.Diagnostics;
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
        [SerializeField, Tooltip("Controls which messages are printed to the Unity console.\n\nNone = silent\nError = errors only\nWarning = errors + warnings\nInfo = normal output\nDebug = verbose")]
        LoggingLevel consoleLogLevel = LoggingLevel.Info;

        static LoggingLevel LoggingLevel = LoggingLevel.Info;

        void Awake() => LoggingLevel = consoleLogLevel;
        private static Type[] _defTypes;
        private static bool _scanned;

        [ThreadStatic] static List<ContentDiagnostic> _threadErrors;

        public IDiagnosticSink DiagnosticSink { get; set; }
        public Action<string> OnProgress { get; set; }

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

                    _threadErrors ??= new List<ContentDiagnostic>();
                    _threadErrors.Add(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.Deserialization,
                        FieldPath = ctx.ErrorContext.Path,
                        Message = ctx.ErrorContext.Error?.Message ?? "Unknown JSON error"
                    });
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
            var sink = DiagnosticSink;
            _registeredCounts.Clear();

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

                OnProgress?.Invoke($"[{modId}] Importing {defType.Name} definitions...");

                var jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);

                // Pass 1: regular definition files
                foreach (var fp in jsonFiles)
                {
                    if (!IsPatchFile(fp))
                        TryImportFile(fp, h, defType, sink);
                }

                // Pass 2: patch files
                using (LoadProfiler.Measure(LoadProfiler.Patches))
                {
                    foreach (var fp in jsonFiles)
                    {
                        if (IsPatchFile(fp))
                            ApplyPatchFile(fp, h, modId, sink);
                    }
                }
            }

            if (LoggingLevel >= LoggingLevel.Info)
            {
                foreach (var kv in _registeredCounts)
                    Debug.Log($"[{modId}] ✔ Registered {kv.Value} {kv.Key} definition(s)");
            }

            // Load sidecar translation files from Translations/*.json
            using (LoadProfiler.Measure(LoadProfiler.Translations))
                ModTranslationLoader.Instance.LoadFromMod(h.Path);
        }

        static readonly Dictionary<string, int> _registeredCounts = new();

        public static bool IsPatchFile(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            return name.EndsWith("_patches", StringComparison.OrdinalIgnoreCase);
        }

        // --------------------------------------------------------------------

        private static void TryImportFile(string filePath, IModHandle h, Type defType, IDiagnosticSink sink = null)
        {
            var modId = Path.GetFileName(h.Path);          // <-- use the mod id, not the file name
            try
            {
                if (LoggingLevel >= LoggingLevel.Debug)
                    Debug.Log($"[{modId}] ▶ Importing {defType.Name}: {filePath}");

                // Clear thread-local errors before deserialization
                _threadErrors?.Clear();

                using var sr = new StreamReader(filePath);
                using var jr = new JsonTextReader(sr) { CloseInput = false };
                JToken token;
                using (LoadProfiler.Measure(LoadProfiler.JsonRead))
                    token = JToken.ReadFrom(jr);

                if (token.Type == JTokenType.Array)
                {
                    foreach (var element in token)
                        DeserializeAndRegister(element, defType, h, filePath, modId, sink);
                }
                else
                {
                    DeserializeAndRegister(token, defType, h, filePath, modId, sink);
                }

                // Drain thread-local serializer errors into the sink
                DrainThreadErrors(sink, modId, filePath);
            }
            catch (JsonReaderException ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Malformed JSON in {filePath}  "
                             + $"(line {ex.LineNumber}, pos {ex.LinePosition}): {ex.Message}");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.JsonParse,
                    ModId = modId,
                    FilePath = filePath,
                    LineNumber = ex.LineNumber,
                    Message = $"Malformed JSON (line {ex.LineNumber}, pos {ex.LinePosition}): {ex.Message}"
                });
            }
            catch (JsonSerializationException ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Serialization error in {filePath}  "
                             + $"(path \"{ex.Path}\"): {ex.Message}");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Deserialization,
                    ModId = modId,
                    FilePath = filePath,
                    FieldPath = ex.Path,
                    Message = $"Serialization error: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Unexpected failure while parsing {filePath}:\n{ex}");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Deserialization,
                    ModId = modId,
                    FilePath = filePath,
                    Message = $"Unexpected failure: {ex.Message}"
                });
            }
        }

        private static void DrainThreadErrors(IDiagnosticSink sink, string modId, string filePath)
        {
            if (sink == null || _threadErrors == null || _threadErrors.Count == 0) return;
            foreach (var err in _threadErrors)
            {
                err.ModId = modId;
                err.FilePath = filePath;
                sink.Report(err);
            }
            _threadErrors.Clear();
        }

        private static void DeserializeAndRegister(
    JToken token, Type defType, IModHandle h, string filePath, string modId, IDiagnosticSink sink = null)
        {
            IContentDef def;
            try
            {
                using (LoadProfiler.Measure(LoadProfiler.Deserialize))
                    def = (IContentDef)token.ToObject(defType, _json);
                def.SourceFile = filePath; // Set the source file for the def
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Failed to deserialize {defType.Name} "
                             + $"in {filePath}: {ex.Message}");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Deserialization,
                    ModId = modId,
                    FilePath = filePath,
                    Message = $"Failed to deserialize {defType.Name}: {ex.Message}"
                });
                return;
            }

            if (def == null || string.IsNullOrWhiteSpace(def.Id))
            {
                if (LoggingLevel >= LoggingLevel.Warning)
                    Debug.LogWarning($"[{modId}] ⚠ Ignored entry in {filePath} – missing \"Id\".");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Deserialization,
                    ModId = modId,
                    FilePath = filePath,
                    Message = $"Entry in {Path.GetFileName(filePath)} missing \"Id\" field.",
                    Detail = "Every content definition must have a non-empty \"Id\" field."
                });
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
            using (LoadProfiler.Measure(LoadProfiler.AssetInject))
                AssetResolver.InjectAssets(def, h.Path, modId, sink);

            ContentCatalogue.Instance.AddOrReplace(def, modId);
            ContentCatalogue.Instance.StoreRawToken(def.Id, token, defType);

            _registeredCounts.TryGetValue(defType.Name, out var count);
            _registeredCounts[defType.Name] = count + 1;

            if (LoggingLevel >= LoggingLevel.Debug)
                Debug.Log($"[{modId}] ✔ Registered {defType.Name} \"{def.Id}\" from {Path.GetFileName(filePath)}");
        }

        // ---- Patch file support ----

        private static void ApplyPatchFile(string filePath, IModHandle h, string modId, IDiagnosticSink sink = null)
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
                        sink?.Report(new ContentDiagnostic
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Category = DiagnosticCategory.Patch,
                            ModId = modId,
                            FilePath = filePath,
                            Message = "Patch entry missing \"$patch\" target ID."
                        });
                        continue;
                    }

                    var opsToken = patchObj["ops"] as JArray;
                    if (opsToken == null || opsToken.Count == 0)
                    {
                        if (LoggingLevel >= LoggingLevel.Warning)
                            Debug.LogWarning($"[{modId}] ⚠ Patch for \"{targetId}\" in {filePath} has no ops.");
                        sink?.Report(new ContentDiagnostic
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Category = DiagnosticCategory.Patch,
                            ModId = modId,
                            FilePath = filePath,
                            DefId = targetId,
                            Message = $"Patch for \"{targetId}\" has no ops."
                        });
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
                        sink?.Report(new ContentDiagnostic
                        {
                            Severity = DiagnosticSeverity.Warning,
                            Category = DiagnosticCategory.Patch,
                            ModId = modId,
                            FilePath = filePath,
                            DefId = targetId,
                            Message = $"Patch target \"{targetId}\" not found in catalogue.",
                            Detail = "Ensure the target def is loaded before this patch. Check load order."
                        });
                        continue;
                    }

                    // Apply patch operations
                    var patched = ContentPatchApplier.Apply(rawToken, patch, sink);

                    // Re-deserialize and re-register
                    DeserializeAndRegister(patched, defType, h, filePath, modId, sink);

                    if (LoggingLevel >= LoggingLevel.Info)
                        Debug.Log(
                            $"[{modId}] ✔ Patched \"{targetId}\" ({patch.Ops.Count} ops) from {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                if (LoggingLevel >= LoggingLevel.Error)
                    Debug.LogError($"[{modId}] ✖ Failed to apply patch file {filePath}:\n{ex}");
                sink?.Report(new ContentDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Patch,
                    ModId = modId,
                    FilePath = filePath,
                    Message = $"Failed to apply patch file: {ex.Message}"
                });
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
