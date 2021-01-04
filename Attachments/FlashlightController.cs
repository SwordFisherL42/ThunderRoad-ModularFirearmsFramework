using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class FlashlightController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        private Light attachedLight;
        private Material flashlightMaterial;
        private Color flashlightEmissionColor;
        private AudioSource activationSound;
        private Handle attachmentHandle;
        private MeshRenderer ignoredMesh;
        private int lightCullingMask;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
            if (module.attachmentRef != null) {
                attachedLight = item.GetCustomReference(module.attachmentRef).GetComponent<Light>();
                if (!String.IsNullOrEmpty(module.flashlightMeshRef))
                {
                    flashlightMaterial = item.GetCustomReference(module.flashlightMeshRef).GetComponent<MeshRenderer>().material;
                    flashlightEmissionColor = flashlightMaterial.GetColor("_EmissionColor");
                    if (!attachedLight.enabled) flashlightMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
            if (!String.IsNullOrEmpty(module.activationSoundRef)) activationSound = item.GetCustomReference(module.activationSoundRef).GetComponent<AudioSource>();
            if (module.attachmentHandleRef != null) attachmentHandle = item.GetCustomReference(module.attachmentRef).GetComponent<Handle>();
            if (module.ignoredMeshRef != null) ignoredMesh = item.GetCustomReference(module.attachmentRef).GetComponent<MeshRenderer>();
            lightCullingMask = 1 << 20;
            lightCullingMask = ~lightCullingMask;
        }

        protected void Start()
        {
            if (ignoredMesh != null)
            {
                ignoredMesh.gameObject.layer = 20; //Set to layer "None"
            }
            if (attachedLight != null)
            {
                attachedLight.cullingMask = lightCullingMask;
            }
        }

        private void ToggleLight()
        {
            if (activationSound != null) activationSound.Play();

            if (attachedLight != null)
            {
                attachedLight.enabled = !attachedLight.enabled;
                if (flashlightMaterial != null)
                {
                    if (attachedLight.enabled) flashlightMaterial.SetColor("_EmissionColor", flashlightEmissionColor);
                    else flashlightMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
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
