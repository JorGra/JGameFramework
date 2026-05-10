using UnityEngine;

namespace JG.Samples
{
    public class Entity : MonoBehaviour
    {
        /// <summary>Runtime Stats container. Defaults pulled from the content registry.</summary>
        public Stats Stats { get; private set; }

        void Awake()
        {
            Stats = new Stats();
        }

        void Start()
        {
            // Direct modifier: +10 power for 5 seconds.
            var directMod = new StatModifier("power", new AddOperation(10f), 5f);
            Stats.Mediator.AddModifier(directMod);
            Debug.Log($"Power after direct modifier: {Stats.GetStat("power")}");

            // Permanent modifier: +20 power.
            var permMod = new StatModifier("power", new AddOperation(20f), 0f);
            Stats.Mediator.AddModifier(permMod);
            Debug.Log($"Power after permanent modifier: {Stats.GetStat("power")}");

            // Remove the temporary one.
            Stats.Mediator.RemoveModifier(directMod);
        }

        void Update()
        {
            Stats.Mediator.Update(Time.deltaTime);
        }
    }
}
