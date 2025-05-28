using UnityEngine;
using JG.Modding;
using Newtonsoft.Json;

public sealed class ExampleContentImporter : MonoBehaviour, IContentImporter
{
    public void Import(IModHandle h)
    {
        // 1. Tiles
        //foreach (var fp in Directory.GetFiles(Path.Combine(h.Path, "Tiles"), "*.json"))
        //    TileCatalog.Instance.AddOrReplace(
        //        JsonConvert.DeserializeObject<TileDef>(File.ReadAllText(fp)));

        Debug.Log($"{h.Path}");
        // 2. Events … repeat for other asset types.
    }
}