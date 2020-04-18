using BS;

namespace FishersFirearmsModular
{
    public class ItemModuleAmmo : ItemModule
    {
        public string bulletMeshID = "bulletMesh";
        // Ammo type allows for prevention of "wrong" ammo being loaded 
        // 1--> Large Bullet, 2--> Small Bullet, 3--> Misc/etc Bullet
        public int ammoType = 1;
        public string handleRef;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemAmmo>();
        }
    }
}
