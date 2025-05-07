namespace JG.Inventory
{
    /// <summary>
    /// Strategy interface – implement one class per gameplay effect.
    /// </summary>
    public interface IItemEffect
    {
        void Apply(InventoryContext context);
        void Remove(InventoryContext context);
    }
}