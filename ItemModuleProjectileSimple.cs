using BS;

namespace FishersFirearmsModular
{
    public class ItemModuleProjectileSimple : ItemModule
    {
        public float lifetime = 1.5f;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemProjectileSimple>();
        }
    }
}
