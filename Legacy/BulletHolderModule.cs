using ThunderRoad;

namespace ShotgunShellHolder
{
    class BulletHolderModule : ItemModule
    {
        public string holderRef = "";
        public string ammoID = "";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<BulletHolderSpawner>();
        }
    }
}
