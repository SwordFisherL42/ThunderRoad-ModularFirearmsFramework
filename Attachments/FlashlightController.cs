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
        //private MeshRenderer ignoredMesh;
        private int lightCullingMask;

        /// General Mechanics ///
        public float lastSpellMenuPress;
        public bool isLongPress = false;
        public bool checkForLongPress = false;
        public bool spellMenuPressed = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
            if (module.flashlightRef != null) {
                attachedLight = item.GetCustomReference(module.flashlightRef).GetComponent<Light>();
                if (!String.IsNullOrEmpty(module.flashlightMeshRef))
                {
                    flashlightMaterial = item.GetCustomReference(module.flashlightMeshRef).GetComponent<MeshRenderer>().material;
                    flashlightEmissionColor = flashlightMaterial.GetColor("_EmissionColor");
                    if (!attachedLight.enabled) flashlightMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
            if (!String.IsNullOrEmpty(module.flashlightActivationSoundRef)) activationSound = item.GetCustomReference(module.flashlightActivationSoundRef).GetComponent<AudioSource>();
            if (module.flashlightHandleRef != null) attachmentHandle = item.GetCustomReference(module.flashlightHandleRef).GetComponent<Handle>();
            //if (module.ignoredMeshRef != null) ignoredMesh = item.GetCustomReference(module.attachmentRef).GetComponent<MeshRenderer>();
            lightCullingMask = 1 << 20;
            lightCullingMask = ~lightCullingMask;
        }

        protected void Start()
        {
            //if (ignoredMesh != null)
            //{
            //    ignoredMesh.gameObject.layer = 20; //Set to layer "None"
            //}
            if (attachedLight != null)
            {
                attachedLight.cullingMask = lightCullingMask;
            }
        }

        protected void StartLongPress()
        {
            checkForLongPress = true;
            lastSpellMenuPress = Time.time;
        }

        public void CancelLongPress()
        {
            checkForLongPress = false;
        }

        public void LateUpdate()
        {
            if (checkForLongPress)
            {
                if (spellMenuPressed)
                {

                    if ((Time.time - lastSpellMenuPress) > module.longPressTime)
                    {
                        // Long Press Detected
                        if (module.longPressToActivate) ToggleLight();
                        CancelLongPress();
                    }

                }
                else
                {
                    // Long Press Self Cancelled (released button before time)
                    // Short Press Detected
                    CancelLongPress();
                    if (!module.longPressToActivate) ToggleLight();
                }
            }

        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(attachmentHandle))
            {
                // "Spell-Menu" Action
                if (action == Interactable.Action.AlternateUseStart)
                {
                    spellMenuPressed = true;
                    StartLongPress();

                }

                if (action == Interactable.Action.AlternateUseStop)
                {
                    spellMenuPressed = false;
                }
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

    }
}
