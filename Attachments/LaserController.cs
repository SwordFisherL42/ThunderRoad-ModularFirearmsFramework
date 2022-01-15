using System;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class LaserController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        private LineRenderer attachedLaser;
        private AudioSource activationSound;
        private LayerMask laserIgnore;
        private Transform laserStart;
        private Transform laserEnd;
        private Transform rayCastPoint;
        private float maxLaserDistance;

        private Handle attachmentHandle;

        private Weapons.BaseFirearmGenerator parentFirearm;
        private Shared.FirearmModule parentModule;

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

            if (!String.IsNullOrEmpty(module.laserRef)) attachedLaser = item.GetCustomReference(module.laserRef).GetComponent<LineRenderer>();

            if (attachedLaser != null)
            {
                if (!String.IsNullOrEmpty(module.laserStartRef)) laserStart = item.GetCustomReference(module.laserStartRef);
                if (!String.IsNullOrEmpty(module.laserEndRef)) laserEnd = item.GetCustomReference(module.laserEndRef);
                if (!String.IsNullOrEmpty(module.laserRayCastPointRef)) rayCastPoint = item.GetCustomReference(module.laserRayCastPointRef);

                LayerMask layermask1 = 1 << 29;
                LayerMask layermask2 = 1 << 28;
                LayerMask layermask3 = 1 << 25;
                LayerMask layermask4 = 1 << 23;
                LayerMask layermask5 = 1 << 9;
                LayerMask layermask6 = 1 << 5;
                LayerMask layermask7 = 1 << 1;
                laserIgnore = layermask1 | layermask2 | layermask3 | layermask4 | layermask5 | layermask6 | layermask7;

                laserIgnore = ~laserIgnore;
                maxLaserDistance = module.maxLaserDistance;
                laserEnd.localPosition = new Vector3(laserEnd.localPosition.x, laserEnd.localPosition.y, laserEnd.localPosition.z);  
            }

            if (!String.IsNullOrEmpty(module.laserActivationSoundRef)) activationSound = item.GetCustomReference(module.laserActivationSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.laserHandleRef)) attachmentHandle = item.GetCustomReference(module.laserHandleRef).GetComponent<Handle>();


        }

        protected void Start()
        {
            if (!module.laserStartActivated && attachedLaser != null)
            {
                attachedLaser.enabled = false;
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
                        if (module.longPressToActivate) ToggleLaser();
                        CancelLongPress();
                    }

                }
                else
                {
                    // Long Press Self Cancelled (released button before time)
                    // Short Press Detected
                    CancelLongPress();
                    if (!module.longPressToActivate) ToggleLaser();
                }
            }

            UpdateLaserPoint();
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

        public void UpdateLaserPoint()
        {
            if (attachedLaser == null) return;
            if (attachedLaser.enabled)
            {
                Ray laserRay = new Ray(rayCastPoint.position, rayCastPoint.forward);

                if (Physics.Raycast(laserRay, out RaycastHit hit, maxLaserDistance, laserIgnore))
                {
                    laserEnd.localPosition = new Vector3(laserEnd.localPosition.x, laserEnd.localPosition.y, rayCastPoint.localPosition.z + hit.distance);
                    //Debug.Log(String.Format("Laser just hit: {0}  Layer: {1}  GOName: {2}", hit.collider.name, hit.collider.gameObject.layer, hit.collider.gameObject.name));
                    AnimationCurve curve = new AnimationCurve();
                    curve.AddKey(0, 0.0075f);
                    curve.AddKey(1, 0.0075f);

                    attachedLaser.widthCurve = curve;
                    attachedLaser.SetPosition(0, laserStart.position);
                    attachedLaser.SetPosition(1, laserEnd.position);
                }
                else
                {
                    laserEnd.localPosition = new Vector3(laserEnd.localPosition.x, laserEnd.localPosition.y, maxLaserDistance);

                    AnimationCurve curve = new AnimationCurve();
                    curve.AddKey(0, 0.0075f);
                    curve.AddKey(1, 0.0f);

                    attachedLaser.widthCurve = curve;
                    attachedLaser.SetPosition(0, laserStart.position);
                    attachedLaser.SetPosition(1, laserEnd.position);
                }

            }

        }

        private void ToggleLaser()
        {
            if (attachedLaser == null) return;
            if (activationSound != null) activationSound.Play();
            attachedLaser.enabled = !attachedLaser.enabled;
        }

    }
}
