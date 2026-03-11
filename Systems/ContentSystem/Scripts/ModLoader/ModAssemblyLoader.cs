#if !UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace JG.Modding
{
    public interface IModAssemblyLoader
    {
        IReadOnlyList<Assembly> LoadAssemblies(IModHandle handle, ModManifest manifest);
    }

    public sealed class ModAssemblyLoader : IModAssemblyLoader
    {
        public IReadOnlyList<Assembly> LoadAssemblies(IModHandle handle, ModManifest manifest)
        {
            var loaded = new List<Assembly>();

            if (manifest.assemblies == null || manifest.assemblies.Length == 0)
                return loaded;

            foreach (var relativePath in manifest.assemblies)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    continue;

                var fullPath = Path.Combine(handle.Path, relativePath);

                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[ModAssemblyLoader] Assembly not found: {fullPath} (mod: {manifest.id})");
                    continue;
                }

                try
                {
                    var assembly = Assembly.LoadFrom(fullPath);
                    loaded.Add(assembly);
                    Debug.Log($"[ModAssemblyLoader] Loaded assembly: {assembly.FullName} from {fullPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModAssemblyLoader] Failed to load assembly {fullPath} (mod: {manifest.id}): {ex.Message}");
                }
            }

            return loaded;
        }
    }

    internal sealed class ModContext : IModContext
    {
        public string ModId { get; }
        public string ModPath { get; }
        public IModServiceProvider Services { get; }

        public ModContext(string modId, string modPath, IModServiceProvider services = null)
        {
            ModId = modId;
            ModPath = modPath;
            Services = services ?? EmptyServiceProvider.Instance;
        }
    }

    /// <summary>Fallback provider that always throws — used when no services are registered.</summary>
    internal sealed class EmptyServiceProvider : IModServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();
        public T Get<T>() where T : class
            => throw new System.InvalidOperationException(
                $"No service registered for type {typeof(T).Name}. No ModServiceRegistry was configured.");
        public bool TryGet<T>(out T service) where T : class { service = null; return false; }
    }
}
#endif
