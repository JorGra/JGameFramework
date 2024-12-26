using UnityEngine;

namespace JG.Samples
{
    public class InterfaceSerializerExample : MonoBehaviour
    {
        public InterfaceReference<IDamageable> damageable;

        private void Start()
        {
            damageable.Value.Damage(10);

            IDamageable d = damageable.Value;
            d.Damage(20);
        }
    }
}