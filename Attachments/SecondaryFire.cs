using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Attachments
{
    public class SecondaryFire : MonoBehaviour
    {
        private float prevShot;
        //ThunderRoad references
        protected Item item;
        protected Shared.AttachmentModule module;
        private Handle secondaryHandle;
        //Unity references
        private AudioSource fireSound;
        private ParticleSystem MuzzleFlash;
        private Transform muzzlePoint;

        public void Awake()
        {
            item = this.GetComponent<Item>();
            item.OnHeldActionEvent += OnHeldAction;
            module = item.data.GetModule<Shared.AttachmentModule>();
            if (!string.IsNullOrEmpty(module.mainGripID)) secondaryHandle = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
            else muzzlePoint = item.transform;
        }

        private void Start()
        {
            prevShot = Time.time;
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(secondaryHandle) && action == Interactable.Action.UseStart && ((Time.time - prevShot) > module.fireDelay))
            {
                prevShot = Time.time;
                Fire();
            }
        }

        public void PreFireEffects()
        {
            if (MuzzleFlash != null) MuzzleFlash.Play();
            if (fireSound != null) fireSound.Play();
        }

        private void Fire()
        {
            PreFireEffects();
            ShootProjectile(item, module.projectileID, muzzlePoint, null, module.forceMult, module.throwMult);
            //TODO: Apply recoil
        }

    }
}
