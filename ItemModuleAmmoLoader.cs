using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleAmmoLoader : ItemModule
    {
        public string bulletMeshID = "bulletMesh";
        public int bulletCount = 5;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemAmmoLoader>();
        }
    }
}
