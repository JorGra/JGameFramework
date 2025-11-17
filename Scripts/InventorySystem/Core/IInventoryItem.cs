using JG.GameContent;
using JG.Inventory;
using System.Collections.Generic;
using UnityEngine;

public interface IInventoryItem : IContentDef   // from the mod system
{
    string DisplayName { get; }
    Sprite Icon { get; }
    int MaxStack { get; }
    IReadOnlyList<string> EquipTags { get; }
    IReadOnlyList<ItemEffectDef> Effects { get; }
}
