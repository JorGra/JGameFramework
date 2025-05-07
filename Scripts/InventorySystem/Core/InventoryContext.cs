
namespace JG.Inventory
{
    /// <summary>
    /// Context object passed to item effects so they can touch world-state.
    /// A single instance is built by <see cref="PlayerEquipmentBridge"/> and
    /// re-used for every Apply / Remove call.
    /// </summary>
    public class InventoryContext
    {
        /// <summary>The Stats container that receives modifiers.</summary>
        public Stats TargetStats { get; set; }

        /// <summary>Optional modifier factory (DI friendly).</summary>
        public IStatModifierFactory ModifierFactory { get; set; }

        /// <summary>Shared wallet example, can stay <c>null</c>.</summary>
        public ResourceWallet Resources { get; set; }
    }
}
