using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleAmmo : ItemModule
    {
        public string handleRef = "bulletHandle";
        public string bulletMeshID = "bulletMesh";
        public int ammoType = 1;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemAmmo>();
        }
    }
}
