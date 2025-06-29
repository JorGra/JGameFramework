using JG.Flyweights;
using System.Collections;
using UnityEngine;


namespace JG.Samples
{
    public class Projectile : Flyweight
    {
        //public new ProjectileSettings settings { get => (ProjectileSettings) base.settings; set => base.settings = value; }
        new ProjectileSettings settings => (ProjectileSettings)base.settings;

        void Update()
        {
            transform.Translate(Vector3.right * (settings.speed * Time.deltaTime));
        }

        IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            FlyweightFactory.ReturnToPool(this);
        }


        private void OnEnable()
        {
            if (settings == null) return;
            StartCoroutine(DespawnAfterDelay(settings.despawnDelay));
        }
    }
}