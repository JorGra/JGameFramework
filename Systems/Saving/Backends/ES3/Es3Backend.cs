#if JG_SAVING_ES3
using System.IO;
using UnityEngine;

namespace JGameFramework.Saving.Backends.ES3
{
    public class Es3Backend : ISaveBackend
    {
        public string Name => "es3-local";

        private readonly string rootPath;
        private readonly string key;
        private readonly string extension;

        public Es3Backend(string rootPath = null, string key = "data", string extension = ".es3")
        {
            this.rootPath = rootPath ?? SavePathResolver.DefaultRoot;
            this.key = key;
            this.extension = extension;
        }

        public void Save<T>(string slotId, string caseId, T value)
        {
            var path = SavePathResolver.Resolve(slotId, caseId, rootPath, extension);
            EnsureDirectory(path);
            global::ES3.Save(key, value, path);
        }

        public T Load<T>(string slotId, string caseId, T defaultValue)
        {
            var path = SavePathResolver.Resolve(slotId, caseId, rootPath, extension);
            if (!global::ES3.FileExists(path) || !global::ES3.KeyExists(key, path))
                return defaultValue;
            return global::ES3.Load<T>(key, path);
        }

        public bool Exists(string slotId, string caseId)
        {
            var path = SavePathResolver.Resolve(slotId, caseId, rootPath, extension);
            return global::ES3.FileExists(path) && global::ES3.KeyExists(key, path);
        }

        public void Delete(string slotId, string caseId)
        {
            var path = SavePathResolver.Resolve(slotId, caseId, rootPath, extension);
            if (global::ES3.FileExists(path))
                global::ES3.DeleteFile(path);
        }

        public void DeleteSlot(string slotId)
        {
            var dir = Path.Combine(rootPath, string.IsNullOrWhiteSpace(slotId) ? "slot_default" : slotId);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }

        private static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
#endif
