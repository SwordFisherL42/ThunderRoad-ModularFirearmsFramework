using BS;

namespace FishersFirearmsModular
{
    public class ItemModuleAmmoResupply : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemAmmoResupply>();
        }
    }
}
