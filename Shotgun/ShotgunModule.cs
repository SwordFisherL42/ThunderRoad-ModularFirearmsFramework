using ThunderRoad;

namespace ModularFirearms.Shotgun
{
    public class ShotgunModule : ItemModule
    {
        // OBSOLETE - USE FOR REFERENCE ONLY //

        //Firearm custom references from Unity Item Definition
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
        public string loadSoundRef;
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
        public string[] acceptedAmmoIDs;
        public int ammoCapacity = 12;

        //Gameplay/Physics/Recoil params
        public float bulletForce = 10.0f;
        public float shellEjectionForce = 5.0f;
        public float hapticForce = 5.0f;
        public float throwMult = 3.0f;

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ShotgunGenerator>();
        }
    }
}
