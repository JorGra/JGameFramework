using UnityEngine;

namespace JG.Samples
{
    /// <summary>
    /// Demo script: reads stats via keys and applies pickup effects at runtime.
    /// </summary>
    public class TestPickUp : MonoBehaviour
    {
        [Tooltip("The player Entity to test against.")]
        [SerializeField] private Entity player;

        [Tooltip("Pickup component that applies a stat bump.")]
        [SerializeField] private Pickup pickup;

        private StatDefinition maxHealthDef;
        private StatDefinition rangeDef;

        void Awake()
        {
            // Cache definitions once
            maxHealthDef = StatRegistryProvider.Instance.Registry.Get("MaxHealth");
            rangeDef = StatRegistryProvider.Instance.Registry.Get("Range");
        }

        void Start()
        {
            Debug.Log($"Starting MaxHealth: {player.Stats.GetStat(maxHealthDef)}");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Give the player a pickup bump
                pickup.ApplyPickupEffect(player);
                Debug.Log($"After pickup MaxHealth: {player.Stats.GetStat(maxHealthDef)}");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                // Just query another stat
                Debug.Log($"Range: {player.Stats.GetStat(rangeDef)}");
            }
        }
    }

}
