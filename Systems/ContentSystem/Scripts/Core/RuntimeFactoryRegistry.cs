using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JG.GameContent;
using JG.GameContent.Diagnostics;
using UnityEngine;

public interface IRuntimeFactory<in TDef> where TDef : IContentDef
{
    void Setup(TDef def);
    GameObject Build(TDef def, Transform parent = null);
}

public static class RuntimeFactoryRegistry
{
    private static readonly Dictionary<Type, object> _byDefType = new();

    public static void Register<TDef>(IRuntimeFactory<TDef> f) where TDef : IContentDef
        => _byDefType[typeof(TDef)] = f;

    public static IRuntimeFactory<TDef> Get<TDef>() where TDef : IContentDef
        => (IRuntimeFactory<TDef>)_byDefType[typeof(TDef)];

    public static void Clear() => _byDefType.Clear();

    public static void SetupAllRegistered(IDiagnosticSink sink = null)
    {
        var catalogue = ContentCatalogue.Instance;
        var getAllMethod = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll));

        foreach (var kvp in _byDefType)
        {
            var defType = kvp.Key;
            var factory = kvp.Value;

            var genericGetAll = getAllMethod.MakeGenericMethod(defType);
            var defs = (System.Collections.IEnumerable)genericGetAll.Invoke(catalogue, null);

            var factoryInterface = typeof(IRuntimeFactory<>).MakeGenericType(defType);
            var setupMethod = factoryInterface.GetMethod("Setup");

            int setupCount = 0;
            foreach (var def in defs)
            {
                var contentDef = (IContentDef)def;
                setupCount++;
                try
                {
                    setupMethod.Invoke(factory, new[] { def });
                }
                catch (TargetInvocationException ex)
                {
                    Debug.LogError($"Failed to setup {defType.Name} '{contentDef.Id}': {ex.InnerException}");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.FactorySetup,
                        DefId = contentDef.Id,
                        Message = $"Factory setup failed for {defType.Name} '{contentDef.Id}': {ex.InnerException?.Message}",
                        Detail = ex.InnerException?.ToString()
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to setup {defType.Name} '{contentDef.Id}': {ex}");
                    sink?.Report(new ContentDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.FactorySetup,
                        DefId = contentDef.Id,
                        Message = $"Factory setup failed for {defType.Name} '{contentDef.Id}': {ex.Message}"
                    });
                }
            }

            Debug.Log($"[RuntimeFactoryRegistry] Set up {setupCount} {defType.Name} definition(s)");
        }
    }
}


public static class RuntimeObjects
{
    public static GameObject Spawn<TDef>(string id, Vector3 pos, Quaternion rot)
        where TDef : class, IContentDef
    {
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def))
            throw new System.Exception($"Def '{id}' ({typeof(TDef).Name}) nicht gefunden");

        var go = RuntimeFactoryRegistry.Get<TDef>().Build(def);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public static GameObject Spawn<TDef>(string id, Transform parent = null)
        where TDef : class, IContentDef
    {
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def))
            throw new System.Exception($"Def '{id}' ({typeof(TDef).Name}) nicht gefunden");
        var go = RuntimeFactoryRegistry.Get<TDef>().Build(def, parent);
        go.SetActive(true);
        return go;
    }

    public static bool TrySpawn<TDef>(string id, Vector3 pos, Quaternion rot, out GameObject go)
        where TDef : class, IContentDef
    {
        go = null;
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def))
            return false;
        go = RuntimeFactoryRegistry.Get<TDef>().Build(def);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return true;
    }

    public static GameObject Get<TDef>(string id)
        where TDef : class, IContentDef
    {
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def))
            throw new System.Exception($"Def '{id}' ({typeof(TDef).Name}) nicht gefunden");
        var go = RuntimeFactoryRegistry.Get<TDef>().Build(def);
        go.SetActive(false);
        return go;
    }
}
