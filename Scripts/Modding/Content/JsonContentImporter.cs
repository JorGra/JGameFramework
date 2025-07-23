using JG.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        private static readonly Type[] _defTypes;
        private static readonly JsonSerializer _json =
            JsonSerializer.Create(new JsonSerializerSettings
            {
                Converters = { new UnityScriptableObjectConverter() },
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                Error = (_, ctx) =>
                {
                    // Fires on every member that fails during Populate/Deserialize
                    if (LoggingLevel >= LoggingLevel.Error)
                        Debug.LogError(
                        $"[JSON] {ctx.ErrorContext.Error}  •  Path: {ctx.ErrorContext.Path}");
                    // If you don’t want the import to halt on *every* error, leave it handled:
                    // ctx.ErrorContext.Handled = true;
                }
            });

        static JsonContentImporter()
        {
            _defTypes = Assembly.GetExecutingAssembly()
                                .GetTypes()
                                .Where(t => !t.IsAbstract &&
                                            typeof(IContentDef).IsAssignableFrom(t) &&
                                            t.GetCustomAttribute<ContentFolderAttribute>() != null)
                                .ToArray();
        }

        public void Import(IModHandle h)
        {
            var modId = Path.GetFileName(h.Path);

            if (LoggingLevel >= LoggingLevel.Info)
                Debug.Log($"[{modId}] Importing content definitions from {h.Path}");
            foreach (var defType in _defTypes)
            {
                var folderAttr = defType.GetCustomAttribute<ContentFolderAttribute>();
                var folderPath = Path.Combine(h.Path, folderAttr.FolderName);
                if (!Directory.Exists(folderPath)) continue;

                if (LoggingLevel >= LoggingLevel.Debug)
                    Debug.Log($"[{modId}] Importing {defType.Name} definitions from {folderPath}");

                foreach (var fp in Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                    TryImportFile(fp, h, defType);
            }
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
            if (LoggingLevel >= LoggingLevel.Info)
                Debug.Log($"[{modId}] ✔ Registered {defType.Name} \"{def.Id}\" from {Path.GetFileName(filePath)}");
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
