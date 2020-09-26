using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleFirearmBase : ItemModule
    {
        //public float slideOffsetZ = 0.025f;
        public float slideNeutralLockOffset = 0.0f;
        public float slideMassOffset = 3.0f;
        public float slideTravelDistance = 0.05f;
        //public float travelLimit = 0.05f;
        //merged refs unity

        //public string configJointRef = "Joint";
        public string slideObjectRef = "SlideObject";
        public string mainHandleRef = "GunGrip";
        public string chamberBulletRef = "ChamberBullet";
        public string muzzlePositionRef = "Muzzle";
        public string shellEjectionRef = "Shell";
        public string flashRef = "Flash";
        
        public string[] soundNames = { "fireSound", "emptySound", "pullSound", "rackSound" };
        public string rackTriggerRef = "RackTrigger";
        public string slideTriggerRef = "SlideTrigger";
        

        public string animationRef;

        //Child Slide
        public int weaponType = 1;
        public string slideRbObject;
        public float slideForwardForce = 50.0f;
        public float slideBlowbackForce = 30.0f;
        

        //public string slideHandleRef = "slideHandle";
        

        //JSON definition references
        public string projectileID;
        public string shellID;
        public string ammoID;
        public string acceptedMagazineID;
        public bool allowGrabMagazineFromGun = false;


        //Firearm custom references from Unity Item Definition


        public string childSlideRef = "ChildSlide";
        public string smokeRef;
        public string soundsRef;

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
            if (weaponType == 1) { item.gameObject.AddComponent<ItemFirearmBase>(); }
            else if (weaponType == 2) { item.gameObject.AddComponent<ItemFirearmBaseSlide>(); }
        }
    }
}
