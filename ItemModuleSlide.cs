using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleSlide : ItemModule
    {
        public float slideForwardForce = 15.0f;
        public float slideBlowbackForce = 30.0f;
        public string slideJointRef = "Joint";
        public string slideHandleRef = "slideHandle";
        public string chamberShellRef = "chamberShell";
        public string chamberBulletRef = "chamberBullet"
            ;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSlide>();
        }
    } 
}
