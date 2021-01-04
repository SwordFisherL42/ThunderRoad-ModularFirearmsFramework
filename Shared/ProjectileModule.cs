using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Shared
{
    public class ProjectileModule : ItemModule
    {

        public float lifetime = 2.0f;
        public int projectileType = 1;

        public float flyingAcceleration = 1.0f;

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
        public int forceMode = 1;

        public ProjectileType GetSelectedType() { return (ProjectileType)FirearmFunctions.weaponTypeEnums.GetValue(projectileType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            ProjectileType selectedType = GetSelectedType();

            if (selectedType.Equals(ProjectileType.Pierce)) { item.gameObject.AddComponent<ItemSimpleProjectile>();}
            else if (selectedType.Equals(ProjectileType.Explosive)) { item.gameObject.AddComponent<ItemSimpleExplosive>(); }
            else { item.gameObject.AddComponent<ItemSimpleProjectile>(); }

        }
    }
}
