using System.IO;
using UnityEngine;

namespace JGameFramework.Saving
{
    /// <summary>
    /// Small helper to keep path generation out of the backend.
    /// </summary>
    public static class SavePathResolver
    {
        public static string DefaultRoot =>
            Path.Combine(Application.persistentDataPath, "slots");

        public static string Resolve(string slotId, string caseId, string customRoot = null, string extension = ".es3")
        {
            var root = string.IsNullOrWhiteSpace(customRoot) ? DefaultRoot : customRoot;
            var safeSlot = string.IsNullOrWhiteSpace(slotId) ? "slot_default" : slotId;
            var safeCase = string.IsNullOrWhiteSpace(caseId) ? "case_default" : caseId;

            var dir = Path.Combine(root, safeSlot);
            var file = $"{safeCase}{extension}";
            return Path.Combine(dir, file);
        }
    }
}
