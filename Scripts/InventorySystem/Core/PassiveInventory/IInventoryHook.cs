namespace JG.Inventory
{
    /// <summary>
    /// Notified whenever items are added to—or removed from—an <see cref="Inventory"/>.
    /// </summary>
    public interface IInventoryHook
    {
        /// <param name="data">Item definition.</param>
        /// <param name="quantity">Number of copies added (+) or removed (−).</param>
        /// <param name="ctx">Stat context; may be <c>null</c>.</param>
        void OnChanged(ItemData data, int quantity, InventoryContext ctx);
    }
}
