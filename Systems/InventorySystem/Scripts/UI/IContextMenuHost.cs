namespace JG.Inventory.UI
{
    /// <summary>
    /// Minimal contract so slot UIs can call back to their owning context menu host.
    /// </summary>
    public interface IContextMenuHost
    {
        void ShowContextMenu(ItemStack stack, UnityEngine.RectTransform anchor);
    }
}
