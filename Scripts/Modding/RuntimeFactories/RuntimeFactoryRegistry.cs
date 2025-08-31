using System;
using System.Collections.Generic;
using JG.GameContent;
using UnityEngine;

public interface IRuntimeFactory<in TDef> where TDef : IContentDef
{
    // set up internal caches, validate 'def'
    void Setup(TDef def);

    // Gets an instance of the object described by 'def'.
    GameObject Build(TDef def, Transform parent = null);
}

public static class RuntimeFactoryRegistry
{
    private static readonly Dictionary<Type, object> _byDefType = new();

    public static void Register<TDef>(IRuntimeFactory<TDef> f) where TDef : IContentDef
        => _byDefType[typeof(TDef)] = f;

    public static IRuntimeFactory<TDef> Get<TDef>() where TDef : IContentDef
        => (IRuntimeFactory<TDef>)_byDefType[typeof(TDef)];
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
