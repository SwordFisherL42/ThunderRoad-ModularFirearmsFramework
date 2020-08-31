using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleSimpleExplosive : ItemModule
    {
        //Range and Force balancing vars
        public float explosiveForce = 1.0f;
        public float blastRadius = 10.0f;
        public float liftMult = 1.0f;
        //Unity prefab referenences
        public string particleEffectRef;
        public string soundRef;
        public string shellMeshRef;
        //Default vars, can be overriden for special cases
        public float lifetime = 10.0f;
        public int forceMode = 1;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSimpleExplosive>();
        }
    }
}
