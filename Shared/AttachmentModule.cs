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
        public float laserToggleHoldTime = 0.5f;
        public string rayCastPointRef;

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
        }
    }
}
