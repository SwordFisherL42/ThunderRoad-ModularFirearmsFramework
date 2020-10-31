using ThunderRoad;
using static ModularFirearms.FirearmFunctions;


namespace ModularFirearms.Shared
{
    public class AmmoModule : ItemModule
    {
        private AmmoType selectedType;
        public string handleRef;
        public string bulletMeshRef;
        public int ammoType = 0;
        public int acceptedAmmoType = 0;
        public int ammoCapacity = 1;
        public float[] ejectionForceVector;

        public bool enableBulletHolder = false;

        public AmmoType GetSelectedType() { return (AmmoType)FirearmFunctions.ammoTypeEnums.GetValue(ammoType); }

        public AmmoType GetAcceptedType() { return (AmmoType)FirearmFunctions.ammoTypeEnums.GetValue(acceptedAmmoType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = GetSelectedType();
            if (selectedType.Equals(AmmoType.Generic) || selectedType.Equals(AmmoType.SemiAuto) || selectedType.Equals(AmmoType.ShotgunShell)) item.gameObject.AddComponent<Items.InteractiveAmmo>();
            else if (selectedType.Equals(AmmoType.Magazine)) item.gameObject.AddComponent<Items.InteractiveMagazine>();
        }

    }
}
