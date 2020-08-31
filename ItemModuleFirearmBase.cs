using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleFirearmBase : ItemModule
    {
        //Manual Interaction settings
        public bool allowGrabMagazineFromGun = false;

        //JSON definition references
        public string projectileID;
        public string shellID;
        public string ammoID;
        public string acceptedMagazineID;

        public string rackTriggerRef = "RackTrigger";
        public string slideTriggerRef = "SlideTrigger";
        public string childSlideRef = "ChildSlide";

        //Firearm custom references from Unity Item Definition
        public string muzzlePositionRef = "Muzzle";
        public string shellEjectionRef = "Shell";
        public string flashRef = "Flash";
        public string mainHandleRef = "GunGrip";
        public string[] soundNames = { "fireSound", "emptySound", "pullSound", "rackSound" };
        public string animationRef;

        public string smokeRef;
        public string soundsRef;

        public int fireMode = 1;
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
            item.gameObject.AddComponent<ItemFirearmBase>();
        }
    }
}
