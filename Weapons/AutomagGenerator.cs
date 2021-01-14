using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Weapons
{
    public class AutomagGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;

        private Light attachedLight;
        private Handle foreGrip;
        /// Ammo Display Controller ///
        private Shared.TextureProcessor ammoCounter;
        private MeshRenderer ammoCounterMesh;
        private Texture2D digitsGridTexture;
        /// Magazine Parameters///
        protected Holder magazineHolder;
        protected Items.InteractiveMagazine insertedMagazine;

        /// Trigger-Zone parameters ///

        /// Slide Interaction ///
        //protected Handle slideHandle;
        //private Shared.ChildRigidbodyController slideController;
        //private GameObject slideObject;
        //private GameObject slideCenterPosition;
        //private ConstantForce slideForce;
        //private Rigidbody slideRB;
        /// Unity Object References ///
        //public ConfigurableJoint connectedJoint;
        protected Handle gunGrip;
        protected Transform muzzlePoint;
        protected Transform shellEjectionPoint;
        protected ParticleSystem muzzleFlash;
        protected ParticleSystem muzzleSmoke;
        protected AudioSource fireSound;

        protected AudioSource fireSound1;
        protected AudioSource fireSound2;
        protected AudioSource fireSound3;
        private int soundCounter = 0;
        private int maxSoundCounter = 0;
        //private AudioSource fireSoundIn;
        //private AudioSource fireSoundLoop;
        //private AudioSource fireSoundOut;

        protected AudioSource emptySound;
        protected AudioSource reloadSound;

        //protected AudioSource pullbackSound;
        //protected AudioSource rackforwardSound;

        protected Animator Animations;
        /// General Mechanics ///
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        public bool isFiring;
        private bool triggerPressed = false;

        /// FireMode Selection and Ammo Tracking //
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;

        void Awake()
        {
            soundCounter = 0;
            maxSoundCounter = 0;
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();

            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);

            //if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.GetCustomReference(module.shellEjectionRef);
            //if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.GetCustomReference(module.animationRef).GetComponent<Animator>();

            //if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            

            if (!String.IsNullOrEmpty(module.fireSound1Ref)) { fireSound1 = item.GetCustomReference(module.fireSound1Ref).GetComponent<AudioSource>(); maxSoundCounter++; soundCounter = 1; }
            if (!String.IsNullOrEmpty(module.fireSound2Ref)) { fireSound2 = item.GetCustomReference(module.fireSound2Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            if (!String.IsNullOrEmpty(module.fireSound3Ref)) { fireSound3 = item.GetCustomReference(module.fireSound3Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            //maxSoundCounter = module.maxFireSounds;
           
            
            //if (fireSound1 != null) maxSoundCounter++;
            //if (fireSound2 != null) maxSoundCounter++;
            //if (fireSound3 != null) maxSoundCounter++;
            

            //if (!String.IsNullOrEmpty(module.fireSoundInRef)) fireSoundIn = item.GetCustomReference(module.fireSoundInRef).GetComponent<AudioSource>();
            //if (!String.IsNullOrEmpty(module.fireSoundLoopRef)) fireSoundLoop = item.GetCustomReference(module.fireSoundLoopRef).GetComponent<AudioSource>();
            //if (!String.IsNullOrEmpty(module.fireSoundOutRef)) fireSoundOut = item.GetCustomReference(module.fireSoundOutRef).GetComponent<AudioSource>();

            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();

            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();

            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();


            if (!String.IsNullOrEmpty(module.flashlightRef)) attachedLight = item.GetCustomReference(module.flashlightRef).GetComponent<Light>();

            if (!String.IsNullOrEmpty(module.foregripHandleRef)) foreGrip = item.GetCustomReference(module.foregripHandleRef).GetComponent<Handle>();
            //Debug.Log("[Fisher-ModularFirearms] AUTOMAG Custom References Complete! !!!");
            //if (!String.IsNullOrEmpty(module.ammoCounterRef))
            //{
            //    //Debug.Log("[Fisher-ModularFirearms] Getting Ammo Counter Objects ...");
            //    ammoCounterMesh = item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
            //    digitsGridTexture = (Texture2D)item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
            //    //Debug.Log("[Fisher-ModularFirearms] GOT Ammo Counter Objects !!!");
            //}

            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);

            //if (module.allowedFireModes != null)
            //{
            //    allowedFireModes = new List<int>(module.allowedFireModes);
            //}

            //if ((digitsGridTexture != null) && (ammoCounterMesh != null))
            //{
            //    ammoCounter = new Shared.TextureProcessor();
            //    ammoCounter.SetGridTexture(digitsGridTexture);
            //    ammoCounter.SetTargetRenderer(ammoCounterMesh);
            //    //Debug.Log("[Fisher-ModularFirearms] Sucessfully Setup Ammo Counter!!");
            //}

            /// Item Events ///
            //Debug.Log("[Fisher-ModularFirearms] AUTOMAG Setup Item events! !!!");
            item.OnHeldActionEvent += OnHeldAction;

            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;

            //Debug.Log("[Fisher-ModularFirearms] AUTOMAG Getting Holder... !!!");
            magazineHolder = item.GetComponentInChildren<Holder>();

            magazineHolder.Snapped += new Holder.HolderDelegate(this.OnMagazineInserted);
            magazineHolder.UnSnapped += new Holder.HolderDelegate(this.OnMagazineRemoved);

            //Debug.Log("[Fisher-ModularFirearms] AUTOMAG All Awake Complete! !!!");

        }

        void Start()
        {

            if (fireSound1 != null) fireSound1.volume = module.soundVolume;
            if (fireSound2 != null) fireSound2.volume = module.soundVolume;
            if (fireSound3 != null) fireSound3.volume = module.soundVolume;

            var magazineData = Catalog.GetData<ItemPhysic>(module.acceptedMagazineID, true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-ModularFirearms][ERROR] No Magazine named " + module.acceptedMagazineID.ToString());
                return;
            }
            else
            {
                magazineData.SpawnAsync(i =>
                {
                    try
                    {
                        magazineHolder.Snap(i);
                        magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    }
                    catch
                    {
                        Debug.Log("[Fisher-Firearms] EXCEPTION IN SNAPPING MAGAZINE ");
                    }
                },
                item.transform.position,
                Quaternion.Euler(item.transform.rotation.eulerAngles),
                null,
                false);
            }

            //if (module.animateSelectionSwitch) SetFireSelectionAnimator(Animations, fireModeSelection);
            if (ammoCounter != null) ammoCounter.DisplayUpdate(0);
        }

        protected void LateUpdate()
        {
            if (!gunGripHeldLeft && !gunGripHeldRight)
            {
                triggerPressed = false;
            }
        }

        public void UpdateAmmoCounter()
        {
            if (ammoCounter == null) return;
            //if (!roundChambered) { ammoCounter.DisplayUpdate(CountAmmoFromMagazine()); }
            else
            {
                ammoCounter.DisplayUpdate(CountAmmoFromMagazine() + 1);
            }
        }

        public void SetAmmoCounter(int value)
        {
            if (ammoCounter != null) { ammoCounter.DisplayUpdate(value); }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (foreGrip != null)
            {
                if (handle.Equals(foreGrip))
                {
                    if ((action == Interactable.Action.AlternateUseStart) || (action == Interactable.Action.UseStart))
                    {
                        if (attachedLight != null) attachedLight.enabled = !attachedLight.enabled;
                        if (emptySound != null) emptySound.Play();
                        //Debug.Log("[ModularFirearms] Toggled Light!");
                        return;
                    }
                }
            }

            // Trigger Action
            if (handle.Equals(gunGrip))
            {
                if (action == Interactable.Action.UseStart)
                {
                    // Begin Firing
                    triggerPressed = true;
                    if (!isFiring) StartCoroutine(FirearmFunctions.GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag));
                }
                if (action == Interactable.Action.UseStop)
                {
                    // End Firing
                    triggerPressed = false;
                }
            }

            // "Spell-Menu" Action
            if (action == Interactable.Action.AlternateUseStart)
            {
                MagazineRelease();
            }

        }

        public void ForceDrop()
        {
            //try { slideHandle.Release(); }
            //catch { }
            //if (slideController != null) slideController.LockSlide();
        }

        public void OnAnyHandleGrabbed(Handle handle, RagdollHand interactor)
        {
            if (handle.Equals(gunGrip))
            {
                //     Debug.Log("[Fisher-ModularFirearms] Main Handle Grabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
            }
        }

        public void OnAnyHandleUngrabbed(Handle handle, RagdollHand interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                //    Debug.Log("[Fisher-ModularFirearms] Main Handle Ungrabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;

            }


        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                insertedMagazine = interactiveObject.GetComponent<Items.InteractiveMagazine>();
                //currentInteractiveObject = interactiveObject;
                //currentInteractiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);
                if (insertedMagazine != null)
                {
                    insertedMagazine.Insert();
                    //item.IgnoreObjectCollision(interactiveObject);
                    // determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    if (insertedMagazine.GetMagazineID() != module.acceptedMagazineID)
                    {
                        // Reject the Magazine with incorrect ID
                        //magazineHolder.UnSnap(interactiveObject);
                        MagazineRelease();
                    }
                    //return;
                }
                else
                {
                    // Reject the non-Magazine object
                    magazineHolder.UnSnap(interactiveObject);
                    insertedMagazine = null;
                }
            }

            catch (Exception e)
            {
                Debug.LogError("[Fisher-ModularFirearms][ERROR] Exception in Adding magazine: " + e.ToString());
            }

            UpdateAmmoCounter();
        }
        protected void OnMagazineRemoved(Item interactiveObject)
        {
            try
            {
                if (insertedMagazine != null)
                {
                    insertedMagazine.Remove();
                    insertedMagazine = null;
                }
                //currentInteractiveObject = null;
            }
            catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }

            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();
        }


        public void MagazineRelease()
        {
            //  Debug.Log("[Fisher-ModularFirearms] Releasing Magazine!");
            try
            {
                if (insertedMagazine != null)
                {
                    insertedMagazine.Eject();
                    insertedMagazine = null;
                }
                //currentInteractiveObject = null;
            }
            catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }

            try
            {
                if (magazineHolder.holdObjects.Count > 0)
                {
                    magazineHolder.UnSnap(magazineHolder.holdObjects[0]);
                }

            }
            catch { }

            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();
        }


        public bool ConsumeOneFromMagazine()
        {
            if (insertedMagazine != null)
            {
                if (insertedMagazine.GetAmmoCount() > 0)
                {
                    insertedMagazine.ConsumeOne();
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public int CountAmmoFromMagazine()
        {
            if (insertedMagazine != null)
            {
                return insertedMagazine.GetAmmoCount();
            }
            else return 0;
        }

        public void SetFiringFlag(bool status)
        {
            isFiring = status;
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public void PlayFireSound()
        {
            if (soundCounter == 0) { return; }
            else if (soundCounter == 1) { fireSound1.Play(); }
            else if (soundCounter == 2) { fireSound2.Play(); }
            else if (soundCounter == 3) { fireSound3.Play(); }
            IncSoundCounter();
        }

        public void IncSoundCounter()
        {
            soundCounter++;
            if (soundCounter > maxSoundCounter) soundCounter = 1;
        }

        public void PreFireEffects()
        {
            FirearmFunctions.Animate(Animations, module.fireAnimationRef);
            if (muzzleFlash != null) muzzleFlash.Play();
            PlayFireSound();
            if (muzzleSmoke != null) muzzleSmoke.Play();
        }

        public bool Fire()
        {
            PreFireEffects();
            if (module.useBuckshot) FirearmFunctions.ProjectileBurst(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false);
            else FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false);
            //Shell Eject, if a shellID 
            FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false);
            FirearmFunctions.ApplyRecoil(item.rb, module.recoilForces, 1.0f, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
            return true;
        }

        protected bool TrackedFire()
        {
            if (ConsumeOneFromMagazine())
            {
                Fire();
                UpdateAmmoCounter();
                return true;
            }
            else return false;

        }

        private IEnumerator StartContiniousFiringSound(AudioSource a, AudioSource entry = null)
        {
            if (entry != null) entry.Play();
            do yield return null;
            while (entry.isPlaying);
            if (triggerPressed)
            {
                a.loop = true;
                a.Play();
            }

        }

        private IEnumerator EndContiniousFiringSound(AudioSource a, AudioSource final = null, bool playFinal = true)
        {
            a.loop = false;
            do yield return null;
            while (a.isPlaying);
            a.Stop();
            if (final != null) final.Play();
            yield return null;
        }
    }
}
