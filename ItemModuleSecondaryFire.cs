using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleSecondaryFire : ItemModule
    {
        //Custom behaviour controls
        public float fireDelay = 1.0f;
        public float forceMult = 100.0f;
        public float throwMult = 1.0f;
        //Unity prefab references
        public string projectileID;
        public string muzzlePositionRef;
        public string fireSoundRef;
        public string muzzleFlashRef;
        public string fireAnim;
        public string mainGripID;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSecondaryFire>();
        }
    }
}
