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
        public float soundVolume = 1.0f;

        public string fireSoundRef;
        public string fireSound1Ref;
        public string fireSound2Ref;
        public string fireSound3Ref;
        public int maxFireSounds = 3;

        public string compassRef;

        public string laserRef;
        public string laserStartRef;
        public string laserEndRef;
        public float maxLaserDistance = 10.0f;
        public bool laserTogglePriority = false;
        public float laserToggleHoldTime = 0.25f;

        public string emptySoundRef;
        public string pullSoundRef;
        public string rackSoundRef;

        public string shellInsertSoundRef;

        public string animationRef;
        public string openAnimationRef;
        public string closeAnimationRef;
        public string fireAnimationRef;

        public bool useBuckshot = false;

        public string rayCastPointRef;

        public string ammoCounterRef;

        public string flashlightRef;
        public string flashlightMeshRef;

        public string foregripHandleRef;

        public float blastRadius = 1.0f;
        public float blastRange = 5.0f;
        public float blastForce = 500.0f;

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
        public string[] acceptedMagazineIDs;

        public bool allowGrabMagazineFromGun = false;
        public int fireMode = 1;
        public int[] allowedFireModes;
        public int fireRate = 600;
        public int burstNumber = 3;
        public bool allowCycleFireMode = false;

        public int maxReceiverAmmo = 12;

        // Gameplay/Physics/Recoil params
        public float bulletForce = 5.0f;
        public float shellEjectionForce = 0.5f;
        public float hapticForce = 5.0f;
        public float throwMult = 2.0f;
        public float[] recoilForces;

        // old Pistol module params
        ///// Common Parameters
        //public string muzzlePointRef;
        //public string muzzleParticleRef;
        //public string projectileID;
        //public string animationRef;
        //public string fireSoundRef;
        //public string handleRef;
        public string idleAnimName = "idle";
        public string overheatAnimName = "overheat";
        public float projectileForce = 1000.0f;
        ///// Type 1 Parameters
        //public string magazineID;

        ///// Type 2 Paramters
        public float energyCapacity = 100.0f;
        public string overheatSoundRef;
        public string chargeLightRef;
        public string chargeEffectRef;
        public string PlasmaChargeUp;
        public string PlasmaChargeLoop;
        public string PlasmaHeatLatch;
        public string PlasmaChargeRelease;


        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);

            selectedType = (WeaponType)FirearmFunctions.weaponTypeEnums.GetValue(firearmCategory);

            if (selectedType.Equals(WeaponType.TestWeapon)) item.gameObject.AddComponent<Weapons.SemiAutoFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.AutoMag)) item.gameObject.AddComponent<Weapons.AutomagGenerator>();
            else if (selectedType.Equals(WeaponType.SemiAuto)) item.gameObject.AddComponent<Weapons.SemiAutoFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.Shotgun)) item.gameObject.AddComponent<Weapons.ShotgunGenerator>();

            else { item.gameObject.AddComponent<Weapons.SemiAutoFirearmGenerator>(); }

            //else if (selectedType.Equals(WeaponType.Shotgun)) item.gameObject.AddComponent<Weapons.ItemShotgunUNSC>();
            //else if (selectedType.Equals(WeaponType.BoltAction)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Revolver)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Sniper)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
            //else if (selectedType.Equals(WeaponType.Sniper)) item.gameObject.AddComponent<Shotgun.ShotgunGenerator>();
        }
    }
}

