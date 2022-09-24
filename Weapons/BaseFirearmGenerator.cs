using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Weapons
{
    public class BaseFirearmGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;

        /// General Mechanics ///
        public float lastSpellMenuPress;
        public bool isLongPress = false;
        public bool checkForLongPress = false;
        bool useRaycast;
        float rayCastMaxDist;
        float raycastForce;
        /// Magazine Parameters///
        protected Holder magazineHolder;
        protected Items.InteractiveMagazine insertedMagazine;
        protected List<string> validMagazineIDs;
        
        /// Trigger-Zone parameters ///
        private float PULL_THRESHOLD;
        private float RACK_THRESHOLD;
        private SphereCollider slideCapsuleStabilizer;

        /// Slide Interaction ///
        protected Handle slideHandle;
        private ChildRigidbodyController slideController;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        private ConstantForce slideForce;
        private Rigidbody slideRB;

        /// Unity Object References ///
        public ConfigurableJoint connectedJoint;
        protected Handle mainHandle;

        protected Transform rayCastPoint;
        protected Transform muzzlePoint;
        protected Transform shellEjectionPoint;
        
        protected ParticleSystem muzzleFlash;
        protected ParticleSystem muzzleSmoke;
        protected ParticleSystem shellParticle;


        protected AudioSource fireSound;
        protected AudioSource fireSound1;
        protected AudioSource fireSound2;
        protected AudioSource fireSound3;
        protected AudioSource emptySound;
        protected AudioSource reloadSound;
        protected AudioSource pullbackSound;
        protected AudioSource rackforwardSound;

        protected Animator animations;

        /// Firearm State Machine ///
        public FireMode fireModeSelection;

        public List<int> allowedFireModes;

        public int ammoCount = 0;

        public bool mainHandleHeldLeft;
        public bool mainHandleHeldRight;
        public bool slideHandleHeldLeft;
        public bool slideHandleHeldRight;
        public bool projectileIsSpawning = false;
        public bool isFiring;

        public bool triggerPressed = false;
        public bool spellMenuPressed = false;
        public bool isRacked = true;
        public bool isPulledBack = false;
        public bool roundChambered = false;

        private bool chamberRoundOnNext = false;
        private bool playSoundOnNext = false;

        private int soundCounter;
        private int maxSoundCounter;

        public bool ProjectileIsSpawning() { return projectileIsSpawning; }

        public void SetProjectileSpawningState(bool newState) { projectileIsSpawning = newState;}

        public void SetFiringFlag(bool status) { isFiring = status; }

        public bool TriggerIsPressed() { return triggerPressed; }

        public void SetAmmoCounter(int value) { ammoCount = value; }

        public int GetAmmoCounter() { return ammoCount; }

        public void SetNextFireMode(FireMode NewFireMode) { fireModeSelection = NewFireMode; }

        public FireMode GetCurrentFireMode() { return fireModeSelection; }

        public void IncSoundCounter()
        {
            soundCounter++;
            if (soundCounter > maxSoundCounter) soundCounter = 1;
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

        public void UpdateAmmoCounter()
        {
            if (!roundChambered) { SetAmmoCounter(CountAmmoFromMagazine()); }
            else { SetAmmoCounter(CountAmmoFromMagazine() + 1); }
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

        public void MagazineRelease()
        {
            try
            {
                if (magazineHolder.items.Count > 0)
                {
                    magazineHolder.UnSnap(magazineHolder.items[0]);
                }
            }
            catch { }
        }

        public void ForceDrop()
        {
            try { slideHandle.Release(); }
            catch { }
            if (slideController != null) slideController.LockSlide();
        }

        public void PlayFireSound()
        {
            if (soundCounter == 0) { return; }
            else if (soundCounter == 1) { fireSound1.Play(); }
            else if (soundCounter == 2) { fireSound2.Play(); }
            else if (soundCounter == 3) { fireSound3.Play(); }
            IncSoundCounter();
        }

        public void PreFireEffects()
        {
            FrameworkCore.Animate(animations, module.fireAnimationRef);
            if (muzzleFlash != null) muzzleFlash.Play();
            PlayFireSound();
            if (muzzleSmoke != null) muzzleSmoke.Play();
        }

        protected bool TrackedFire()
        {
            if (slideController != null)
            {
                if (slideController.IsLocked()) return false;
            }
            if (!roundChambered) return false;
            // Round cycle sequence
            roundChambered = false;
            slideController.ChamberRoundVisible(roundChambered);
            Fire();
            if (ConsumeOneFromMagazine())
            {
                roundChambered = true;
                slideController.ChamberRoundVisible(roundChambered);
                slideController.BlowBack();
            }
            else
            {
                isRacked = false;
                isPulledBack = true;
                chamberRoundOnNext = true;
                // playSoundOnNext = true;
                slideController.LastShot();
            }
            UpdateAmmoCounter();
            return true;
        }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null) slideRB = slideObject.AddComponent<Rigidbody>();
            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;
            // Stabilizing Collider on Slide Object
            slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = stabilizerRadius;
            slideCapsuleStabilizer.gameObject.layer = 21;
            Physics.IgnoreLayerCollision(21, 12);
            Physics.IgnoreLayerCollision(21, 15);
            Physics.IgnoreLayerCollision(21, 22);
            Physics.IgnoreLayerCollision(21, 23);
            // Creating Config Joint and Setting Joint Values
            slideForce = slideObject.AddComponent<ConstantForce>();
            connectedJoint = item.gameObject.AddComponent<ConfigurableJoint>();
            connectedJoint.connectedBody = slideRB;
            connectedJoint.anchor = new Vector3(0, 0, -0.5f * module.slideTravelDistance);
            connectedJoint.axis = Vector3.right;
            connectedJoint.autoConfigureConnectedAnchor = false;
            connectedJoint.connectedAnchor = Vector3.zero; // new Vector3(0.04f, -0.1f, -0.22f);
            connectedJoint.secondaryAxis = Vector3.up;
            connectedJoint.xMotion = ConfigurableJointMotion.Locked;
            connectedJoint.yMotion = ConfigurableJointMotion.Locked;
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            connectedJoint.angularXMotion = ConfigurableJointMotion.Locked;
            connectedJoint.angularYMotion = ConfigurableJointMotion.Locked;
            connectedJoint.angularZMotion = ConfigurableJointMotion.Locked;
            connectedJoint.linearLimit = new SoftJointLimit { limit = 0.5f * module.slideTravelDistance, bounciness = 0.0f, contactDistance = 0.0f };
            connectedJoint.massScale = 1.0f;
            connectedJoint.connectedMassScale = module.slideMassOffset;
        }

        public bool SlideToggleLock()
        {
            if ((insertedMagazine != null) && (insertedMagazine.GetAmmoCount() <= 0)) return false;

            if (slideController != null)
            {
                // If the slide is locked back and there is a loaded magazine inserted, load the next round
                if (slideController.IsLocked())
                {
                    if (ConsumeOneFromMagazine())
                    {
                        roundChambered = true;
                    }
                    chamberRoundOnNext = false;
                    playSoundOnNext = false;
                    isRacked = true;
                    isPulledBack = false;
                    slideController.ForwardState();
                    if (rackforwardSound != null) rackforwardSound.Play();
                    UpdateAmmoCounter();
                    return true;
                }
                // If the slide is held back by the player and not yet locked, lock it
                else if (slideController.IsHeld() && isPulledBack)
                {
                    slideController.LockedBackState();
                    if (emptySound != null) emptySound.Play();

                    return true;
                }
                else return false;
            }
            else return false;
        }

        private void Fire(bool firedByNPC = false, bool playEffects = true)
        {
            if (playEffects) PreFireEffects();
            if (firedByNPC) return;
            if (!useRaycast || !ShootRaycastDamage(muzzlePoint, raycastForce, rayCastMaxDist))
            {
                ShootProjectile(
                item,
                module.projectileID,
                muzzlePoint,
                GetItemSpellChargeID(item),
                module.bulletForce,
                module.throwMult,
                false,
                slideCapsuleStabilizer,
                SetProjectileSpawningState
                );
            }
            ApplyRecoil(item.rb, module.recoilForces, module.throwMult, mainHandleHeldLeft, mainHandleHeldRight, module.hapticForce, module.recoilTorques);
            if (shellParticle != null)
            {
                GameObject newShell = GameObject.Instantiate(shellParticle.gameObject, null);
                newShell.transform.position = shellParticle.transform.position;
                newShell.transform.rotation = shellParticle.transform.rotation;
                newShell.transform.parent = null;
                ParticleSystem newShellParticle = newShell.GetComponent<ParticleSystem>();
                if (newShellParticle != null)
                {
                    var main = newShellParticle.main;
                    main.stopAction = ParticleSystemStopAction.Destroy;
                    newShellParticle.Play();
                }
            }
        }

        void Awake()
        {
            soundCounter = 0;
            maxSoundCounter = 0;
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();
            // Prioritize local settings, then fetch global settings //
            useRaycast = module.useHitscan ? true : Shared.FrameworkSettings.local.useHitscan;
            rayCastMaxDist = module.useHitscan ? module.hitscanMaxDistance : Shared.FrameworkSettings.local.hitscanMaxDistance;
            if (rayCastMaxDist <= 0f) rayCastMaxDist = Mathf.Infinity;
            raycastForce = module.bulletForce * module.hitscanForceMult;
            DisableCulling(item);
            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.rayCastPointRef)) rayCastPoint = item.GetCustomReference(module.rayCastPointRef);
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.animationRef)) animations = item.GetCustomReference(module.animationRef).GetComponent<Animator>();
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.fireSound1Ref)) { fireSound1 = item.GetCustomReference(module.fireSound1Ref).GetComponent<AudioSource>(); maxSoundCounter++; soundCounter = 1; }
            if (!String.IsNullOrEmpty(module.fireSound2Ref)) { fireSound2 = item.GetCustomReference(module.fireSound2Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            if (!String.IsNullOrEmpty(module.fireSound3Ref)) { fireSound3 = item.GetCustomReference(module.fireSound3Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.shellParticleRef)) shellParticle = item.GetCustomReference(module.shellParticleRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) mainHandle = item.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();
            lastSpellMenuPress = 0.0f;
            RACK_THRESHOLD = -0.1f * module.slideTravelDistance;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;
            fireModeSelection = (FireMode)Enum.Parse(typeof(FireMode), module.fireMode);
            validMagazineIDs = new List<string>(module.acceptedMagazineIDs);
            /// Item Events ///
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;
            magazineHolder = item.GetComponentInChildren<Holder>();
            magazineHolder.Snapped += new Holder.HolderDelegate(this.OnMagazineInserted);
            magazineHolder.UnSnapped += new Holder.HolderDelegate(this.OnMagazineRemoved);
        }

        void Start()
        {
            if (fireSound1 != null) fireSound1.volume = module.soundVolume;
            if (fireSound2 != null) fireSound2.volume = module.soundVolume;
            if (fireSound3 != null) fireSound3.volume = module.soundVolume;

            /// 1) Create and Initialize configurable joint between the base and slide
            /// 2) Create and Initialize the slide controller object
            /// 3) Setup the slide controller into the default state
            /// 4) Spawn and Snap in the inital magazine
            /// 5) (optional) Set the firemode selection switch to the correct position
            InitializeConfigurableJoint(module.slideStabilizerRadius);
            slideController = new ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);
            if (slideController == null) Debug.LogError("[Fisher-ModularFirearms] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();
            var magazineData = Catalog.GetData<ItemData>(module.acceptedMagazineIDs[0], true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-ModularFirearms][ERROR] No Magazine named " + module.acceptedMagazineIDs[0].ToString());
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
                        Debug.Log("[Fisher-ModularFirearms] EXCEPTION IN SNAPPING MAGAZINE ");
                    }
                },
                item.transform.position,
                Quaternion.Euler(item.transform.rotation.eulerAngles),
                null,
                false);
            }
        }

        protected void LateUpdate()
        {
            if (checkForLongPress)
            {
                if (spellMenuPressed)
                {
                    
                    if ((Time.time - lastSpellMenuPress) > module.longPressTime)
                    {
                        // Long Press Detected
                        if (module.longPressToEject) MagazineRelease();
                        CancelLongPress();
                    }
                }
                else
                {
                    // Long Press Self Cancelled (released button before time)
                    // Short Press Detected
                    CancelLongPress();
                    if (!module.longPressToEject) MagazineRelease();
                }  
            }
            if (!mainHandleHeldLeft && !mainHandleHeldRight)
            {
                triggerPressed = false;
                if (slideController != null) { slideController.LockSlide(); }
            }
            if ((slideObject.transform.localPosition.z <= PULL_THRESHOLD) && !isPulledBack)
            {
                if (slideController != null)
                {
                    if (slideController.IsHeld())
                    {
                        // Entered PulledBack position
                        if (pullbackSound != null) pullbackSound.Play();
                        isPulledBack = true;
                        isRacked = false;
                        playSoundOnNext = true;
                        if (!roundChambered)
                        {
                            chamberRoundOnNext = true;
                            UpdateAmmoCounter();
                        }
                        else
                        {
                            FrameworkCore.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer);
                            roundChambered = false;
                            chamberRoundOnNext = true;
                        }
                        slideController.ChamberRoundVisible(false);
                    }
                }
            }
            if ((slideObject.transform.localPosition.z > (PULL_THRESHOLD - RACK_THRESHOLD)) && isPulledBack)
            {
                // Show chamber ammo
                if (CountAmmoFromMagazine() > 0) { slideController.ChamberRoundVisible(true); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {
                // Entered Rack position"
                isRacked = true;
                isPulledBack = false;

                if (chamberRoundOnNext)
                {
                    if (ConsumeOneFromMagazine())
                    {
                        slideController.ChamberRoundVisible(true);
                        chamberRoundOnNext = false;
                        roundChambered = true;
                    }
                }
                if (playSoundOnNext)
                {
                    if (rackforwardSound != null) rackforwardSound.Play();
                    playSoundOnNext = false;
                }
                UpdateAmmoCounter();
            }
            if (slideController != null) slideController.FixCustomComponents();
            else return; // TODO: Remove this return, so we initialize even if we don't fix custom components
            if (slideController.initialCheck) return;
            try
            {
                if (mainHandleHeldRight || mainHandleHeldLeft)
                {
                    slideController.UnlockSlide();
                    slideController.initialCheck = true;
                }
            }
            catch { Debug.Log("[Fisher-ModularFirearms] Slide EXCEPTION"); }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(mainHandle))
            {
                // Trigger Action
                if (action == Interactable.Action.UseStart)
                {
                    // Begin Firing
                    triggerPressed = true;
                    if (!isFiring) StartCoroutine(FrameworkCore.GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag, ProjectileIsSpawning));
                }
                if (action == Interactable.Action.UseStop)
                {
                    // End Firing
                    triggerPressed = false;
                }
                // "Spell-Menu" Action
                if (action == Interactable.Action.AlternateUseStart)
                {
                    spellMenuPressed = true;
                    if (SlideToggleLock()) { return; }
                    StartLongPress();
                }
                if (action == Interactable.Action.AlternateUseStop)
                {
                    spellMenuPressed = false;
                }
            }
            if (action == Interactable.Action.Grab)
            {
                if (handle.Equals(mainHandle))
                {
                    if (interactor.playerHand == Player.local.handRight) mainHandleHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) mainHandleHeldLeft = true;

                    if ((mainHandleHeldRight || mainHandleHeldLeft) && (slideController != null)) slideController.UnlockSlide();
                }

                if (handle.Equals(slideHandle))
                {
                    if (interactor.playerHand == Player.local.handRight) slideHandleHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) slideHandleHeldLeft = true;
                    if (slideController != null) slideController.SetHeld(true);
                    slideController.ForwardState();
                }
            }
            if (action == Interactable.Action.Ungrab)
            {
                if (handle.Equals(mainHandle))
                {
                    if (interactor.playerHand == Player.local.handRight) mainHandleHeldRight = false;
                    if (interactor.playerHand == Player.local.handLeft) mainHandleHeldLeft = false;
                    if (!mainHandleHeldRight && !mainHandleHeldLeft)
                    {
                        if (interactor.playerHand == Player.local.handRight) slideHandleHeldRight = false;
                        if (interactor.playerHand == Player.local.handLeft) slideHandleHeldLeft = false;
                        if (((slideController != null))) { slideController.LockSlide(); }
                        ForceDrop();
                    }
                }
                if (handle.Equals(slideHandle))
                {
                    if (slideController != null) slideController.SetHeld(false);
                }
            }
        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                interactiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);
                insertedMagazine = interactiveObject.GetComponent<Items.InteractiveMagazine>();

                if (insertedMagazine != null)
                {
                    insertedMagazine.Insert();
                    // Determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    Handle h = interactiveObject.GetComponentInChildren<Handle>();
                    if (h != null) h.data.disableTouch = !module.allowGrabMagazineFromGun;
                    string insertedMagID = insertedMagazine.GetMagazineID();
                    bool keepMagazine = false;
                    foreach (string magazineID in validMagazineIDs)
                    {
                        if (magazineID.Equals(insertedMagID))
                        {
                            keepMagazine = true;
                            break;
                        }
                    }
                    if (!keepMagazine)
                    {
                        Debug.LogWarning("[Fisher-ModularFirearms] " + item.name + " REJECTED MAGAZINE " + insertedMagID.ToString() + ". Allowed Magazines are:  "+ string.Join(",", validMagazineIDs.ToArray()));
                        // Reject the Magazine with incorrect ID
                        MagazineRelease();
                    }
                }
                else
                {
                    Debug.LogWarning("[Fisher-ModularFirearms] Rejected MAGAZINE Due to NULL InteractiveMagazine Object");
                    // Reject the non-Magazine object
                    magazineHolder.UnSnap(interactiveObject);
                    insertedMagazine = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Fisher-ModularFirearms][ERROR] Exception in Adding magazine: " + e.ToString());
            }

            if (roundChambered) UpdateAmmoCounter();
        }

        protected void OnMagazineRemoved(Item interactiveObject)
        {
            try
            {
                if (insertedMagazine != null)
                {
                    Handle h = interactiveObject.GetComponentInChildren<Handle>();
                    if (h != null) h.data.disableTouch = false;
                    insertedMagazine.Eject(item);
                    insertedMagazine = null;
                }
            }
            catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }
            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();
        }

        public void OnAnyHandleGrabbed(Handle handle, RagdollHand interactor)
        {
            if (handle.Equals(mainHandle))
            {
                // Handle Grabbed
                if (interactor.playerHand == Player.local.handRight) mainHandleHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) mainHandleHeldLeft = true;
                if ((mainHandleHeldRight || mainHandleHeldLeft) && (slideController != null)) slideController.UnlockSlide();
            }
            if (handle.Equals(slideHandle))
            {
                // Slide Grabbed
                if (interactor.playerHand == Player.local.handRight) slideHandleHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) slideHandleHeldLeft = true;
                slideController.SetHeld(true);
                slideController.ForwardState();
            }
        }

        public void OnAnyHandleUngrabbed(Handle handle, RagdollHand interactor, bool throwing)
        {
            if (handle.Equals(mainHandle))
            {
                // Handle Ungrabbed
                if (interactor.playerHand == Player.local.handRight) mainHandleHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) mainHandleHeldLeft = false;
                if (!mainHandleHeldRight && !mainHandleHeldLeft)
                {
                    if (((slideController != null))) { slideController.LockSlide(); }
                    ForceDrop();
                }
            }
            if (handle.Equals(slideHandle))
            {
                // Slide Ungrabbed
                if (interactor.playerHand == Player.local.handRight) slideHandleHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) slideHandleHeldLeft = false;
                slideController.SetHeld(false);
            }
        }
    }
}
