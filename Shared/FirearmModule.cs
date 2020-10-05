using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Shared
{
    public class FirearmModule : ItemModule
    {
        private WeaponType selectedType;
        public int firearmCategory = 0;

        public string mainHandleRef;
        public string slideHandleRef;
        public string slideCenterRef;

        public string muzzlePositionRef;
        public string shellEjectionRef;
        public string chamberBulletRef;
        public string flashRef;
        public string smokeRef;

        public string fireSoundRef;
        public string emptySoundRef;
        public string pullSoundRef;
        public string rackSoundRef;

        public string animationRef;
        public string openAnimationRef;
        public string closeAnimationRef;

        // Distance the child slide/bolt can be travel
        public float slideTravelDistance = 0.05f;

        // Slide defaults, don't edit these except for edge cases or special behaviours
        public float slideStabilizerRadius = 0.02f;
        public float slideRackThreshold = -0.01f;
        public float slideNeutralLockOffset = 0.0f;
        public float slideMassOffset = 1.0f;
        public float slideForwardForce = 50.0f;
        public float slideBlowbackForce = 30.0f;

        // JSON definition references
        public string projectileID;
        public string shellID;
        public string ammoID;
        public string acceptedMagazineID;
        public bool allowGrabMagazineFromGun = false;
        public int fireMode = 1;
        public int[] allowedFireModes;
        public int fireRate = 600;
        public int burstNumber = 3;
        public bool allowCycleFireMode = false;

        public int maxReceiverAmmo = 12;

        // Gameplay/Physics/Recoil params
        public float bulletForce = 10.0f;
        public float shellEjectionForce = 5.0f;
        public float hapticForce = 5.0f;
        public float throwMult = 3.0f;
        public float[] recoilForces;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            selectedType = (WeaponType) FirearmFunctions.weaponTypeEnums.GetValue(firearmCategory);
            if (selectedType.Equals(WeaponType.TestWeapon)) item.gameObject.AddComponent<SemiAuto.TestFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.SemiAuto)) item.gameObject.AddComponent<SemiAuto.SemiAutoFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.Shotgun)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.BoltAction)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Revolver)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Sniper)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Sniper)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
        }
    }
        
}

