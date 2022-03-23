using System;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Shared
{
    public class ProjectileModule : ItemModule
    {
        public string CustomSplatterEffect;

        public float lifetime = 2.0f;
        public string projectileType = "Pierce";

        public float flyingAcceleration = 1.0f;
        public float throwMult = 1.0f;

        //Type 1 params
        public bool allowFlyTime = true;
        public bool useHitScanning = false;

        //Type 2 params
        //Range and Force balancing vars
        public float explosiveForce = 1.0f;
        public float blastRadius = 10.0f;
        public float liftMult = 1.0f;
        //Unity prefab referenences
        public string particleEffectRef;
        public string soundRef;
        public string shellMeshRef;
        //Default vars, can be overriden for special cases
        public string forceMode = "Impulse";

        public ProjectileType GetSelectedType(string projectileType) { return (ProjectileType)Enum.Parse(typeof(ProjectileType), projectileType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            ProjectileType selectedType = GetSelectedType(projectileType);

            if (selectedType.Equals(ProjectileType.Pierce) || selectedType.Equals(ProjectileType.Blunt)) { item.gameObject.AddComponent<Projectiles.BasicProjectile>(); }
            else if (selectedType.Equals(ProjectileType.Explosive)) { item.gameObject.AddComponent<Projectiles.ExplosiveProjectile>(); }
            else { item.gameObject.AddComponent<Projectiles.BasicProjectile>(); }
        }
    }
}
