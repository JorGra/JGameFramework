using JG.Inventory;

namespace JG.Inventory
{
    /// <summary>
    /// Context object passed to item effects (player, world, stats, etc.).
    /// Extend as needed.
    /// </summary>
    public class InventoryContext
    {
        //public PlayerStats TargetStats { get; set; }
        public ResourceWallet Resources { get; set; }
        // … add more references (AudioManager, VFXSpawner) if effects need them
    }
}
