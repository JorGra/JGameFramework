using System.IO;
using UnityEngine;

namespace JG.Modding
{
    /// <summary>Resolves where the mods folder lives on the current platform.</summary>
    public static class ModPaths
    {
        /// <summary>
        /// Absolute path of the mods folder. Next to the project/build on desktop;
        /// on WebGL the root of the in-memory filesystem, filled by ModsVfsPreload.jspre
        /// before the engine starts.
        /// </summary>
        public static string GetModsRoot(string folder = "Mods")
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "/" + folder;
#else
            string projectOrBuildRoot =
                Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectOrBuildRoot, folder);
#endif
        }
    }
}
