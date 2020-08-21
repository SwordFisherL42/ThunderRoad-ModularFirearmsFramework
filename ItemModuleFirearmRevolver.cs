using ThunderRoad;

namespace ModularFirearms
{
    public class ItemModuleFirearmRevolver : ItemModule
    {
        //Basic settings
        public int ammoType = 1;
        public bool lockFiringToAnimation = true;
        public bool spinToRandomBullet = false;
        public float firingDelay = 0.015f;
        public float minGestureVelocity = 0.75f;
        public bool gestureEnabled = true;

        //JSON definition references
        public string projectileID;

        //item custom references
        public string muzzlePositionRef = "Muzzle";
        public string flashRef = "Flash";
        public string animatorRef = "Animations";
        public string[] soundNames = {"FireSound", "EmptySound", "ReloadSound" };
        public string gunGripID = "Haptics";
        public float throwMult = 3.0f;

        // Animation name references
        public float animationSpeed;
        public string fireAnimPrefix = "fire_";
        public string idleAnimPrefix = "idle_";
        public string closingAnim = "close_cylinder";
        public string openingAnim = "open_cylinder";
        public string idleOpenAnim = "cylinderIsOpen";

        // Phyics/gameplay params
        public float bulletForce = 7.0f;
        public float hapticForce = 5.0f;
        public float recoilMult = 1.0f;
        public float[] recoilForces = { 0f, 0f, 100f, 150f, -1500f, -1000f };  // x-min, x-max, y-min, y-max, z-min, z-max

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemFirearmRevolver>();
        }
    }
}
