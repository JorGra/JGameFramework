using JG.Util.Extensions;
using UnityEngine;
using JG.Flyweights;



namespace JG.Samples { 

    [CreateAssetMenu(fileName = "ProjectileSettings", menuName = "Samples/Gameplay/Flyweight/Projectile Settings", order = 1)]
    public class ProjectileSettings : FlyweightSettings
    {
        public float despawnDelay = 5f;
        public float speed = 10f;
        public float damage = 10f;

        public override Flyweight Create()
        {
            var go = Instantiate(prefab);
            go.name = prefab.name;

            var flyweight = go.GetOrAdd<Projectile>();
            flyweight.settings = this;

            go.SetActive(false);
            return flyweight;
        }
    }
}