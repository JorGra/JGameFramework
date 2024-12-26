using UnityEngine;

namespace JG.Samples
{
    [CreateAssetMenu(fileName ="DamageableAsset", menuName ="Samples/SerializedInterface/IDamageable")]
    public class DamageableAsset : ScriptableObject, IDamageable
    {
        public void Damage(float amount)
        {
            Debug.Log($"DamageableAsset: {amount}");
        }
    }
}