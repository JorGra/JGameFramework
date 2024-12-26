using UnityEngine;

namespace JG.Samples
{
    public class DamageableComponent : MonoBehaviour, IDamageable
    {
        public void Damage(float amount)
        {
            Debug.Log($"DamageableComponent: {amount}");
        }
    }
}