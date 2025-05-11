namespace JG.Inventory
{
    /// <summary>
    /// Optional hook that runs whenever items are added to—or removed from—an
    /// inventory.  Lets us plug extra behaviour without duplicating list logic.
    /// </summary>
    public interface IInventoryHook
    {
        /// <param name="stack">The stack that entered or left.</param>
        /// <param name="ctx">Context (stats, world, …) passed through by caller.</param>
        /// <param name="added">True = stack was added; False = removed.</param>
        void OnChanged(ItemStack stack, InventoryContext ctx, bool added);
    }
}
