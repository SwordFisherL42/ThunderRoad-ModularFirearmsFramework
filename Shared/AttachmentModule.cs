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
        public string ignoredMeshRef;
        public int attachmentType = 0;
        private AttachmentType selectedType;
        public AttachmentType GetSelectedType() { return (AttachmentType)FirearmFunctions.attachmentTypeEnums.GetValue(attachmentType); }

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            selectedType = GetSelectedType();
            if (selectedType.Equals(AttachmentType.Flashlight)) item.gameObject.AddComponent<FlashlightController>();
        }
    }
}
