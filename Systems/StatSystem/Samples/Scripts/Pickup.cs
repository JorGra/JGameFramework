using UnityEngine;

namespace JG.Samples
{


    public abstract class Pickup : MonoBehaviour
    {
        public abstract void ApplyPickupEffect(Entity entity);

        public void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Entity>())
            {
                ApplyPickupEffect(other.GetComponent<Entity>());
                Destroy(gameObject);
            }

        }
    }
}