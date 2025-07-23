using JG.Modding;
using Newtonsoft.Json;
using UnityEngine;

public sealed class ExampleContentImporter : MonoBehaviour, IContentImporter
{
    public void Import(IModHandle h)
    {
        // 1. Items
        //foreach (var fp in Directory.GetFiles(Path.Combine(h.Path, "Items"), "*.json"))
        //    Catalog.Instance.AddOrReplace(
        //        JsonConvert.DeserializeObject<ItemDef>(File.ReadAllText(fp)));

        Debug.Log($"{h.Path}");
        // 2. Events … repeat for other asset types.
    }
}