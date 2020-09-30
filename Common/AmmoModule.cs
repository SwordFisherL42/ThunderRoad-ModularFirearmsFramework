using ThunderRoad;

namespace ModularFirearms.Common
{
    public class AmmoModule : ItemModule
    {
        public string handleRef;
        public string bulletMeshID;
        public int ammoType = 1;
        public int numberOfRounds = 1;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ModularAmmo>();
        }
    }
}
