using ThunderRoad;

namespace ModularFirearms.SemiAuto
{
    public class SemiAutoModule : ItemModule
    {
        //Firearm custom references from Unity Item Definition
        public int weaponType = 1;
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

        // Distance the child slide/bolt can be pulled back
        public float slideTravelDistance = 0.05f;
        // Slide defaults, don't edit these except for edge cases or special behaviours
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

        // Gameplay/Physics/Recoil params
        public float bulletForce = 10.0f;
        public float shellEjectionForce = 5.0f;
        public float hapticForce = 5.0f;
        public float throwMult = 3.0f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            if (weaponType == 1) item.gameObject.AddComponent<SemiAutoFirearmGenerator>();
            if (weaponType == 2) item.gameObject.AddComponent<TestFirearmGenerator>();
        }
    }
}
