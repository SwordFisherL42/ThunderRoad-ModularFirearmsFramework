using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleSimpleProjectile : ItemModule
    {
        public float lifetime = 1.5f;
        public bool allowFlyTime = true;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemSimpleProjectile>();
        }
    }
}
