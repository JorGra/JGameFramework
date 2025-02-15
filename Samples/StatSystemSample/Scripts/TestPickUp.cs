using UnityEngine;

namespace JG.Samples
{
    public class TestPickUp : MonoBehaviour
    {
        [SerializeField] private Entity player;
        [SerializeField] private Pickup pickup;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log(player.Stats.GetStat(GameStatDefinitions.MaxHealth));
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                pickup.ApplyPickupEffect(player);
                Debug.Log(player.Stats.GetStat(GameStatDefinitions.MaxHealth));
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log(player.Stats.GetStat(GameStatDefinitions.Range));
            }
        }
    }
}
