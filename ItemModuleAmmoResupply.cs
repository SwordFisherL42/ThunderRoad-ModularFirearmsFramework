using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleAmmoResupply : ItemModule
    {
        public string magazineID = "MagazineBasicPistol";
        public bool despawnBagOnEmpty = false;
        public int capacity = 0;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            // item.gameObject.AddComponent<ItemAmmoResupply>(); 
            item.gameObject.AddComponent<Items.ItemInfintieAmmo>();
        }
    }
}
