using System;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class CompassController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        private GameObject compass;
        private int currentIndex;
        private int compassIndex;
        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            if (!String.IsNullOrEmpty(module.compassRef)) compass = item.GetCustomReference(module.compassRef).gameObject;
            if (compass != null)
            {
                currentIndex = -1;
                compassIndex = 0;
            }
        }
        public void LateUpdate()
        {
            UpdateCompassPosition();
        }
        public void UpdateCompassPosition()
        {
            if (compass == null) return;
            compassIndex = (int)Mathf.Floor(compass.transform.rotation.eulerAngles.y / 45.0f);
            if (currentIndex != compassIndex)
            {
                currentIndex = compassIndex;
                compass.transform.Rotate(0, 0, -1.0f * compass.transform.rotation.eulerAngles.z, Space.Self);
                compass.transform.Rotate(0, 0, compassIndex * 45.0f, Space.Self);
            }
        }
    }
}
