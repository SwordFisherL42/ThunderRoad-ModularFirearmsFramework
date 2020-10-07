using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Shared
{
    public class FlashlightController : MonoBehaviour
    {
        protected Item item;
        protected AttachmentModule module;
        private Light flashlightSource;
        private Handle attachmentHandle;
        private MeshRenderer ignoredMesh;
        private int lightCullingMask;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
            if (module.attachmentRef != null) flashlightSource = item.definition.GetCustomReference(module.attachmentRef).GetComponent<Light>();
            if (module.attachmentHandleRef != null) attachmentHandle = item.definition.GetCustomReference(module.attachmentRef).GetComponent<Handle>();
            if (module.ignoredMeshRef != null) ignoredMesh = item.definition.GetCustomReference(module.attachmentRef).GetComponent<MeshRenderer>();
            lightCullingMask = 1 << 20;
            lightCullingMask = ~lightCullingMask;
        }

        protected void Start()
        {
            if (ignoredMesh != null)
            {
                ignoredMesh.gameObject.layer = 20; //Set to layer "None"
            }
            if (flashlightSource != null)
            {
                flashlightSource.cullingMask = lightCullingMask;
            }
        }
        public void ToggleLight()
        {
            if (flashlightSource != null) flashlightSource.enabled = !flashlightSource.enabled;
        }

        public void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(attachmentHandle))
            {
                if (action == Interactable.Action.AlternateUseStart)
                {
                    ToggleLight();
                }
            }
        }

    }
}
