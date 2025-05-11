using System.Collections;
using System.Collections.Generic;
using JG.Inventory.UI;
using UnityEngine;

namespace JG.Inventory.Samples
{
    [DefaultExecutionOrder(-1)]
    public class InventoryStatsEntity : MonoBehaviour, IStatsProvider
    {
        Stats stats;
        
        public Stats Stats => stats;

        // Start is called before the first frame update
        void Start()
        {
            stats = new Stats();
        }

        // Update is called once per frame
        void Update()
        {
            stats.Mediator.Update(Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var h = stats.GetStat(StatRegistryProvider.Instance.Registry.Get("health"));
                var a = stats.GetStat(StatRegistryProvider.Instance.Registry.Get("armor"));

                Debug.Log(gameObject.name + "   " + h + " " + a);
            }
        }
    }
}