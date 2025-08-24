namespace JG.Inventory
{
    /// <summary>
    /// Strategy interface – implement one class per gameplay effect.
    /// </summary>
    public interface IItemEffect
    {
        void Apply(IInventoryContext context);
        void Remove(IInventoryContext context);
    }
}