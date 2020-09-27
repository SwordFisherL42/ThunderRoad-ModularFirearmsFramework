using ThunderRoad;

namespace ModularFirearms
{
    public class SemiAutoModule : ItemModule
    {
        public float triggerCCOffset = 0.07f;
        //Firearm custom references from Unity Item Definition
        public string muzzlePositionRef = "Muzzle";
        public string flashRef = "Flash";
        public string shellEjectionRef = "Shell";
        public string mainHandleRef = "GunGrip";
        public string slideHandleRef = "SlideObject";
        public string chamberBulletRef = "ChamberBullet";

        public string fireSoundRef = "fireSound";
        public string emptySoundRef = "emptySound";
        public string pullSoundRef = "pullSound";
        public string rackSoundRef = "rackSound";

        public string slideFrontTrigger = "SlideFront";
        public string slideRearTrigger = "SlideBack";
        public string slideCenterRef;
        public string smokeRef;
        public string animationRef;

        // Distance the child slide/bolt can be pulled back
        public float slideTravelDistance = 0.05f;
        // Slide defaults, don't edit these except for edge cases or special behaviours
        public float slideNeutralLockOffset = 0.0f;
        public float slideMassOffset = 3.0f;
        public float slideForwardForce = 50.0f;
        public float slideBlowbackForce = 30.0f;

        //JSON definition references
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

        //Gameplay/Physics/Recoil params
        public float bulletForce = 10.0f;
        public float shellEjectionForce = 5.0f;
        public float hapticForce = 5.0f;
        public float throwMult = 3.0f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<SemiAutoFirearmGenerator>();
        }
    }
}
