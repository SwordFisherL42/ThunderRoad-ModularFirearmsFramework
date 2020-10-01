using ThunderRoad;
using static ModularFirearms.FirearmFunctions;


namespace ModularFirearms.Common
{
    public class AmmoModule : ItemModule
    {
        public string handleRef;
        public string bulletMeshID;
        public int ammoType = 0;
        public int numberOfRounds = 1;

        public AmmoType GetSelectedType() { return (AmmoType)FirearmFunctions.ammoTypeEnums.GetValue(ammoType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<InteractiveAmmo>();
        }

        
    }
}
