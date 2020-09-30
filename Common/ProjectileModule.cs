using ThunderRoad;


namespace ModularFirearms.Common
{
    public class ProjectileModule : ItemModule
    {
        public float lifetime = 2.0f;
        public int projectileType = 1; // 1-> Pierce, 2->Explosive, 3->Energy, 4->Blunt/Buck

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

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            if (projectileType == 1)
            {
                item.gameObject.AddComponent<ItemSimpleProjectile>();
            }
            else if (projectileType == 2) { item.gameObject.AddComponent<ItemSimpleExplosive>(); }
            else { item.gameObject.AddComponent<ItemSimpleProjectile>(); }

        }
    }
}
