using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Shared
{
    public class AttachmentModule : ItemModule
    {
        public string attachmentRef;
        public string attachmentHandleRef;
        public string activationSoundRef;
        public string ignoredMeshRef;
        public string flashlightMeshRef;
        public string laserRef;
        public string laserStartRef;
        public string laserEndRef;

        public float maxLaserDistance = 10.0f;
        public bool laserTogglePriority = false;
        public float laserToggleHoldTime = 0.25f;
        public string rayCastPointRef;

        //Secondary fire
        //Custom behaviour controls
        public float fireDelay = 1.0f;
        public float forceMult = 100.0f;
        public float throwMult = 1.0f;
        //Unity prefab references
        public string projectileID;
        public string muzzlePositionRef;
        public string fireSoundRef;
        public string muzzleFlashRef;
        public string fireAnim;
        public string mainGripID;

        public int attachmentType = 0;
        private AttachmentType selectedType;
        public AttachmentType GetSelectedType() { return (AttachmentType)FirearmFunctions.attachmentTypeEnums.GetValue(attachmentType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = GetSelectedType();
            if (selectedType.Equals(AttachmentType.Flashlight)) item.gameObject.AddComponent<Attachments.FlashlightController>();
            else if (selectedType.Equals(AttachmentType.Laser)) item.gameObject.AddComponent<Attachments.LaserController>();
            else if (selectedType.Equals(AttachmentType.GrenadeLauncher)) item.gameObject.AddComponent<Attachments.GrenadeLauncherController>();
            else if (selectedType.Equals(AttachmentType.SecondaryFire)) item.gameObject.AddComponent<Attachments.SecondaryFire>();
        }
    }
}
