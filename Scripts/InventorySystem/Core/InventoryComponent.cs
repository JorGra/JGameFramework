using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Holds the player's <see cref="Inventory"/> and can seed it from one or
    /// more JSON TextAssets.  Each file may contain:
    ///
    /// • exactly ONE object  →  { "id":"potion", "quantity":3 }  
    /// • or MANY objects     →  { "items":[ {…}, {…} ] }
    /// </summary>
    public class InventoryComponent : MonoBehaviour
    {
        public Inventory Runtime { get; private set; }

        [Header("Starter Item Files (TextAssets)")]
        [SerializeField] private List<TextAsset> starterFiles = new();

        void Awake()
        {
            Runtime = new Inventory();

            foreach (var (data, qty) in StarterItemParser.ParseMany(starterFiles))
                Runtime.AddItem(data, qty);
        }
    }
}
