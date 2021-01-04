using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;

            if (!String.IsNullOrEmpty(module.attachmentRef)) attachedLaser = item.GetCustomReference(module.attachmentRef).GetComponent<LineRenderer>();

            if (attachedLaser != null)
            {
                if (!String.IsNullOrEmpty(module.laserStartRef)) laserStart = item.GetCustomReference(module.laserStartRef);
                if (!String.IsNullOrEmpty(module.laserEndRef)) laserEnd = item.GetCustomReference(module.laserEndRef);
                if (!String.IsNullOrEmpty(module.rayCastPointRef)) rayCastPoint = item.GetCustomReference(module.rayCastPointRef);
                //laserInfKeyframe = attachedLaser.widthCurve.keys[1];
                //laserIgnore = 1 << 20;
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

            if (!String.IsNullOrEmpty(module.activationSoundRef)) activationSound = item.GetCustomReference(module.activationSoundRef).GetComponent<AudioSource>();
            if (module.attachmentHandleRef != null) attachmentHandle = item.GetCustomReference(module.attachmentRef).GetComponent<Handle>();

        }

        public void LateUpdate()
        {
            UpdateLaserPoint();
        }

        protected void Start()
        {


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
                    //attachedLaser.widthCurve.keys[1] = attachedLaser.widthCurve.keys[0];

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
                    //attachedLaser.widthCurve.keys[1] = laserInfKeyframe;
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

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(attachmentHandle))
            {
                if (action == Interactable.Action.AlternateUseStart)
                {
                    ToggleLaser();
                }
            }
        }

    }
}
