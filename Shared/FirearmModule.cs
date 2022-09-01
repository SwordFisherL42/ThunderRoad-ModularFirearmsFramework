using System;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Shared
{
    public class FirearmModule : ItemModule
    {
        private WeaponType selectedType;
        public int firearmCategory = 0;
        public string firearmType = "SemiAuto";

        public string mainHandleRef;
        public string slideHandleRef;
        public string slideCenterRef;

        public string muzzlePositionRef;
        public string shellEjectionRef;
        public string chamberBulletRef;
        public string flashRef;
        public string shellParticleRef;

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

        public bool longPressToEject = false;
        public float longPressTime = 0.25f;

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
        //public string acceptedMagazineID;
        public string[] acceptedMagazineIDs;

        public bool allowGrabMagazineFromGun = false;
        //public int fireMode = 1;
        public string fireMode = "Single";
        public int[] allowedFireModes;
        public int fireRate = 600;
        public int burstNumber = 3;
        public bool allowCycleFireMode = false;

        public int maxReceiverAmmo = 12;
        public string shellReceiverDef;

        // Gameplay/Physics/Recoil params
        public float bulletForce = 10.0f;
        public float shellEjectionForce = 0.5f;
        public float hapticForce = 5.0f;
        public float throwMult = 2.0f;
        public float[] recoilForces;
        public float[] recoilTorques;

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

        // Unity prefab references
        public string muzzleFlashRef;
        public string swtichSoundRef;
        public string reloadSoundRef;
        public string animatorRef;
        public string fireAnim;
        public string emptyAnim;
        public string reloadAnim;
        //public string shellEjectionRef;

        // Item definition references
        public string mainGripID;

        // NPC settings
        public string npcRaycastPositionRef;
        public float npcDistanceToFire = 10.0f;
        public bool npcMeleeEnableOverride = true;
        public float npcDamageToPlayer = 1.0f;
        public float npcDetectionRadius = 100f;
        public float npcMeleeEnableDistance = 0.5f;

        // Custom Behaviour Settings
        public bool loopedFireSound = false;
        public int ammoCapacity = 0;

        // Flintlock weapon settings
        public bool isFlintlock = false;        // Set weapon to Flintlock mode
        public bool waitForReloadAnim = false;  // Do not allow actions while Reload Animation is playing
        public bool waitForFireAnim = false;    // Wait for the Fire animation to finish before shooting the projectile/playing primary effects
        public string earlyFireSoundRef;        // First sound played (flintlock activation)
        public string earlyMuzzleFlashRef;      // First particle played (flintlock activation)
        public float flintlockDelay = 1.0f;     // Delay between PreFire effects and actual Fire (with main fire effects)

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = (WeaponType)Enum.Parse(typeof(WeaponType), firearmType);
            if (selectedType.Equals(WeaponType.SemiAuto)) item.gameObject.AddComponent<Weapons.BaseFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.Shotgun)) item.gameObject.AddComponent<Weapons.ShotgunGenerator>();
            else if (selectedType.Equals(WeaponType.TestWeapon)) item.gameObject.AddComponent<Weapons.BaseFirearmGenerator>();
            else if (selectedType.Equals(WeaponType.SimpleFirearm)) item.gameObject.AddComponent<Weapons.SimpleFirearm>();
            else { item.gameObject.AddComponent<Weapons.BaseFirearmGenerator>(); }
        }
    }
}

