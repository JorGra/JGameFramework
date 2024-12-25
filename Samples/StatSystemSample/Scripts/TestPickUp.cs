using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Samples
{

    public class TestPickUp : MonoBehaviour
    {
        [SerializeField] Entity player;
        [SerializeField] Pickup pickup;
        // Start is called before the first frame update
        void Start()
        {
            Debug.Log(player.Stats.GetStat(StatType.MaxHealth));
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                pickup.ApplyPickupEffect(player);
                Debug.Log(player.Stats.GetStat(StatType.MaxHealth));
            }


            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log(player.Stats.GetStat(StatType.Range));
            }
        }
    }

}