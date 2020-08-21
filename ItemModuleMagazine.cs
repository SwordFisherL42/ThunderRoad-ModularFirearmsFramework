using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleMagazine : ItemModule
    {

        public string bulletMeshRef = "bulletMesh";
        public string handleRef = "magazineHandle";

        public float[] ejectionForceVector = { 0f, 10f, 10f };
        public int acceptedAmmoType = 2; //By default take 9mm ammo
        public int ammoCapacity = 10;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemMagazine>();
        }
    }
}
