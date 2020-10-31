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
        protected ObjectHolder magazineHolder;
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
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);

            //if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.definition.GetCustomReference(module.shellEjectionRef);
            //if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.definition.GetCustomReference(module.animationRef).GetComponent<Animator>();

            //if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            

            if (!String.IsNullOrEmpty(module.fireSound1Ref)) { fireSound1 = item.definition.GetCustomReference(module.fireSound1Ref).GetComponent<AudioSource>(); maxSoundCounter++; soundCounter = 1; }
            if (!String.IsNullOrEmpty(module.fireSound2Ref)) { fireSound2 = item.definition.GetCustomReference(module.fireSound2Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            if (!String.IsNullOrEmpty(module.fireSound3Ref)) { fireSound3 = item.definition.GetCustomReference(module.fireSound3Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            //maxSoundCounter = module.maxFireSounds;
           
            
            //if (fireSound1 != null) maxSoundCounter++;
            //if (fireSound2 != null) maxSoundCounter++;
            //if (fireSound3 != null) maxSoundCounter++;
            

            //if (!String.IsNullOrEmpty(module.fireSoundInRef)) fireSoundIn = item.definition.GetCustomReference(module.fireSoundInRef).GetComponent<AudioSource>();
            //if (!String.IsNullOrEmpty(module.fireSoundLoopRef)) fireSoundLoop = item.definition.GetCustomReference(module.fireSoundLoopRef).GetComponent<AudioSource>();
            //if (!String.IsNullOrEmpty(module.fireSoundOutRef)) fireSoundOut = item.definition.GetCustomReference(module.fireSoundOutRef).GetComponent<AudioSource>();

            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();

            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.definition.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();

            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.definition.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();


            if (!String.IsNullOrEmpty(module.flashlightRef)) attachedLight = item.definition.GetCustomReference(module.flashlightRef).GetComponent<Light>();

            if (!String.IsNullOrEmpty(module.foregripHandleRef)) foreGrip = item.definition.GetCustomReference(module.foregripHandleRef).GetComponent<Handle>();
            //Debug.Log("[Fisher-GreatJourney] AUTOMAG Custom References Complete! !!!");
            //if (!String.IsNullOrEmpty(module.ammoCounterRef))
            //{
            //    //Debug.Log("[Fisher-GreatJourney] Getting Ammo Counter Objects ...");
            //    ammoCounterMesh = item.definition.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
            //    digitsGridTexture = (Texture2D)item.definition.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
            //    //Debug.Log("[Fisher-GreatJourney] GOT Ammo Counter Objects !!!");
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
            //    //Debug.Log("[Fisher-GreatJourney] Sucessfully Setup Ammo Counter!!");
            //}

            /// Item Events ///
            //Debug.Log("[Fisher-GreatJourney] AUTOMAG Setup Item events! !!!");
            item.OnHeldActionEvent += OnHeldAction;

            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;

            //Debug.Log("[Fisher-GreatJourney] AUTOMAG Getting Holder... !!!");
            magazineHolder = item.GetComponentInChildren<ObjectHolder>();

            magazineHolder.Snapped += new ObjectHolder.HolderDelegate(this.OnMagazineInserted);
            magazineHolder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnMagazineRemoved);

            //Debug.Log("[Fisher-GreatJourney] AUTOMAG All Awake Complete! !!!");

        }

        void Start()
        {

            if (fireSound1 != null) fireSound1.volume = module.soundVolume;
            if (fireSound2 != null) fireSound2.volume = module.soundVolume;
            if (fireSound3 != null) fireSound3.volume = module.soundVolume;

            var magazineData = Catalog.GetData<ItemPhysic>(module.acceptedMagazineID, true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-GreatJourney][ERROR] No Magazine named " + module.acceptedMagazineID.ToString());
                return;
            }
            else
            {
                magazineHolder.Snap(magazineData.Spawn(true));
            }
            magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;

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

        public void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            if (foreGrip != null)
            {
                if (handle.Equals(foreGrip))
                {
                    if ((action == Interactable.Action.AlternateUseStart) || (action == Interactable.Action.UseStart))
                    {
                        if (attachedLight != null) attachedLight.enabled = !attachedLight.enabled;
                        if (emptySound != null) emptySound.Play();
                        //Debug.Log("[GreatJourney] Toggled Light!");
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

        public void OnAnyHandleGrabbed(Handle handle, Interactor interactor)
        {
            if (handle.Equals(gunGrip))
            {
                //     Debug.Log("[Fisher-GreatJourney] Main Handle Grabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
            }
        }

        public void OnAnyHandleUngrabbed(Handle handle, Interactor interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                //    Debug.Log("[Fisher-GreatJourney] Main Handle Ungrabbed!");
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
                Debug.LogError("[Fisher-GreatJourney][ERROR] Exception in Adding magazine: " + e.ToString());
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
            catch { Debug.LogWarning("[Fisher-GreatJourney] Unable to Eject the Magazine!"); }

            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();
        }


        public void MagazineRelease()
        {
            //  Debug.Log("[Fisher-GreatJourney] Releasing Magazine!");
            try
            {
                if (insertedMagazine != null)
                {
                    insertedMagazine.Eject();
                    insertedMagazine = null;
                }
                //currentInteractiveObject = null;
            }
            catch { Debug.LogWarning("[Fisher-GreatJourney] Unable to Eject the Magazine!"); }

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
