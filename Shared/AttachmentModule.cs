using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Shared
{
    public class AttachmentModule : ItemModule
    {
        // Module Flow Control
        private AttachmentType selectedType;
        //public string attachmentType;
        public string[] attachmentTypes;
        public AttachmentType GetSelectedType(string attachmentType) { return (AttachmentType)Enum.Parse(typeof(AttachmentType), attachmentType); }

        // General References
        public string attachmentRef;
        public string attachmentHandleRef;
        public string activationSoundRef;
        public string ignoredMeshRef;
        public string rayCastPointRef;

        public bool longPressToActivate = true;
        public float longPressTime = 0.25f;

        // FireMode Switch Attachment
        public string swtichRef;
        public string switchActivationSoundRef;
        public string swtichHandleRef;

        public string[] allowedFireModes = { };
        public string[] switchPositionRefs = { };

        // Laser Pointer Attachment 
        public string laserRef;
        public string laserStartRef;
        public string laserEndRef;
        public float maxLaserDistance = 10.0f;
        public string laserHandleRef;
        public string laserRayCastPointRef;
        public string laserActivationSoundRef;
        public bool laserStartActivated = true;

        // Ammo Counter Attachment
        public string ammoCounterRef;

        // Compass Attachment
        public string compassRef;

        // Flashlight Attachment
        public string flashlightRef;
        public string flashlightHandleRef;
        public string flashlightActivationSoundRef;
        public string flashlightMeshRef;

        // Secondary Fire Attachment
        public float fireDelay = 1.0f;
        public float forceMult = 100.0f;
        public float throwMult = 1.0f;
        public string projectileID;
        public string muzzlePositionRef;
        public string fireSoundRef;
        public string muzzleFlashRef;
        public string fireAnim;
        public string mainGripID;

        // Old Integer Enum Parsing Method
        //public int attachmentType = 0;
        //public AttachmentType GetSelectedType() { return (AttachmentType)FirearmFunctions.attachmentTypeEnums.GetValue(attachmentType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            foreach (string attachmentType in attachmentTypes)
            {
                selectedType = GetSelectedType(attachmentType); 
                if (selectedType.Equals(AttachmentType.Flashlight)) item.gameObject.AddComponent<Attachments.FlashlightController>();
                else if (selectedType.Equals(AttachmentType.Laser)) item.gameObject.AddComponent<Attachments.LaserController>();
                else if (selectedType.Equals(AttachmentType.Compass)) item.gameObject.AddComponent<Attachments.CompassController>();
                else if (selectedType.Equals(AttachmentType.AmmoCounter)) item.gameObject.AddComponent<Attachments.AmmoCounterController>();
                else if (selectedType.Equals(AttachmentType.FireModeSwitch)) item.gameObject.AddComponent<Attachments.FireModeSwitchController>();
                else if (selectedType.Equals(AttachmentType.GrenadeLauncher)) item.gameObject.AddComponent<Attachments.GrenadeLauncherController>();
                else if (selectedType.Equals(AttachmentType.SecondaryFire)) item.gameObject.AddComponent<Attachments.SecondaryFire>();
            }

        }
    }
}
