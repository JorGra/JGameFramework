using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{

    /// <summary>
    /// Minimal contract so <see cref="ItemSlotUI"/> can call back to whatever
    /// list owns it (regular or passive inventory) without a hard type-coupling.
    /// </summary>
    public interface IContextMenuHost
    {
        void ShowContextMenu(ItemStack stack, UnityEngine.RectTransform anchor);
    }


    /// <summary>Right-click / pad-button menu for inventory & equipment Slots.</summary>
    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] Button useBtn, equipBtn, unequipBtn, dropBtn;

        ItemStack stack;
        Inventory inv;
        EquipmentSlotComponent equipSlot;
        EquipmentHub hub;
        IStatsProvider statsProvider;      // NEW

        public void Open(ItemStack s,
                         Inventory i,
                         RectTransform anchor,
                         EquipmentHub hub,
                         EquipmentSlotComponent slotComponent = null,
                         IStatsProvider provider = null)   // NEW (optional)
        {
            stack = s;
            inv = i;
            equipSlot = slotComponent;
            this.hub = hub;
            statsProvider = provider;

            /* --- button visibility (unchanged) --- */
            bool canEquip = !IsEquipped && stack.Data.EquipTags.Count > 0 && this.hub != null;
            bool canUse = stack.Data.Effects.Count > 0 && stack.Data.EquipTags.Count == 0;
            bool canUnequip = IsEquipped;

            equipBtn.gameObject.SetActive(canEquip);
            useBtn.gameObject.SetActive(canUse);
            unequipBtn.gameObject.SetActive(canUnequip);

            /* --- callbacks --- */
            useBtn.Set(OnUse);
            equipBtn.Set(OnEquip);
            unequipBtn.Set(OnUnequip);
            dropBtn.Set(OnDrop);

            ((RectTransform)transform).position =
                anchor.position + new Vector3(GetWidth() * .5f, 0);

            gameObject.SetActive(true);
        }

        /* ----- operations ----- */

        void OnUse()
        {
            if (hub != null)
            {
                hub.Use(stack);               // classic inventory path
            }
            else if (inv != null)
            {
                inv.UseItem(stack.Data.Id,
                            new InventoryContext { TargetStats = statsProvider?.Stats });
            }
            Close();
        }

        void OnEquip() { hub?.Equip(stack); Close(); }
        void OnUnequip()
        {
            if (hub == null) { Debug.LogError("Bridge null on Unequip"); return; }
            hub.Unequip(equipSlot);
            Close();
        }

        void OnDrop()
        {
            inv.RemoveItem(stack.Data.Id, stack.Count);
            Close();
        }

        /* ----- helpers ----- */

        public void Close() => gameObject.SetActive(false);
        float GetWidth() => ((RectTransform)transform).rect.width;
        bool IsEquipped => equipSlot != null;
    }
}



/* thin helpers ----------------------------------------------------------- */
static class BtnExt
{
    public static void Set(this Button b, UnityEngine.Events.UnityAction cb)
    {
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(cb);
    }
}
