using System;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Shared
{
    public class AmmoModule : ItemModule
    {
        private AmmoType selectedType;
        public string handleRef;
        public string bulletMeshRef;
        public string ammoType = "SemiAuto";
        public string acceptedAmmoType = "SemiAuto";
        public int ammoCapacity = 1;
        public float[] ejectionForceVector;

        public string magazineID = "MagazineBasicPistol";
        public bool despawnBagOnEmpty = false;
        public bool disableCulling = false;

        public bool enableBulletHolder = false;

        public AmmoType GetSelectedType() { return (AmmoType)Enum.Parse(typeof(AmmoType), ammoType); }

        public AmmoType GetAcceptedType() { return (AmmoType)Enum.Parse(typeof(AmmoType), acceptedAmmoType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = GetSelectedType();
            if (selectedType.Equals(AmmoType.Generic) || selectedType.Equals(AmmoType.SemiAuto) || selectedType.Equals(AmmoType.ShotgunShell)) item.gameObject.AddComponent<Items.InteractiveAmmo>();
            else if (selectedType.Equals(AmmoType.Magazine)) item.gameObject.AddComponent<Items.InteractiveMagazine>();
            else if (selectedType.Equals(AmmoType.Pouch)) item.gameObject.AddComponent<Items.AmmoResupply>();
        }
    }
}
