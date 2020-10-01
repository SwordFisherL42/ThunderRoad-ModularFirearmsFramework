using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.SemiAuto
{
    public class TestFirearmGenerator : MonoBehaviour
    {
        /// ThunderRoad Object References ///
        protected Item item;
        protected Common.FirearmModule module;
        protected ObjectHolder magazineHolder;
        protected ItemMagazine insertedMagazine;

        /// Trigger-Zone parameters ///
        private float PULL_THRESHOLD;
        private float RACK_THRESHOLD;
        private SphereCollider slideCapsuleStabilizer;

        /// Slide Interaction ///
        protected Handle slideHandle;
        //private SemiAutoSlide slideController;
        private Common.ChildRigidbodyController slideController;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        private ConstantForce slideForce;
        private Rigidbody slideRB;

        /// Unity Object References ///
        public ConfigurableJoint connectedJoint;
        protected Handle gunGrip;
        protected Transform muzzlePoint;
        protected Transform shellEjectionPoint;
        protected ParticleSystem muzzleFlash;
        protected ParticleSystem muzzleSmoke; 
        protected AudioSource fireSound;
        protected AudioSource emptySound;
        protected AudioSource reloadSound;
        protected AudioSource pullbackSound;
        protected AudioSource rackforwardSound;
        protected Animator Animations;

        /// General Mechanics ///
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        public bool isFiring;
        private bool triggerPressed = false;
        private bool isRacked = true;
        private bool isPulledBack = false;
        private bool chamberRoundOnNext = false;
        private bool roundChambered = false;
        private bool playSoundOnNext = false;
        /// FireMode Selection and Ammo Tracking //
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Common.FirearmModule>();

            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.definition.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.definition.GetCustomReference(module.animationRef).GetComponent<Animator>();
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.definition.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.definition.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.definition.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.definition.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.definition.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.definition.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();

            RACK_THRESHOLD = module.slideRackThreshold;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;

            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);

            if (module.allowedFireModes != null)
            {
                allowedFireModes = new List<int>(module.allowedFireModes);
            }

            /// Item Events ///
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;
            item.OnSnapEvent += OnFirearmSnapped;
            item.OnUnSnapEvent += OnFirearmUnSnapped;
            magazineHolder = item.GetComponentInChildren<ObjectHolder>();
            magazineHolder.Snapped += new ObjectHolder.HolderDelegate(this.OnMagazineInserted);
            magazineHolder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnMagazineRemoved);

        }

        protected void Start()
        {
            /// 1) Create and Initialize configurable joint between the base and slide
            /// 2) Create and Initialize the slide controller object
            /// 3) Setup the slide controller into the default state
            /// 4) Spawn and Snap in the inital magazine
            /// 5) (optional) Set the firemode selection switch to the correct position
            InitializeConfigurableJoint(module.slideStabilizerRadius);

            slideController = new Common.ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);

            if (slideController == null) Debug.LogError("[Fisher-Firearms] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();

            var magazineData = Catalog.GetData<ItemPhysic>(module.acceptedMagazineID, true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No Magazine named " + module.acceptedMagazineID.ToString());
                return;
            }
            else
            {
                magazineHolder.Snap(magazineData.Spawn(true));
            }
            magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;

            SetFireSelectionAnimator(Animations, fireModeSelection);

            return;
        }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null)
            {
                // TODO: Figure out why adding RB from code doesnt work
                slideRB = slideObject.AddComponent<Rigidbody>();
                Debug.Log("[Fisher-Firearms][Config-Joint-Init] CREATED Rigidbody ON SlideObject...");
                
            }
            else { Debug.Log("[Fisher-Firearms][Config-Joint-Init] ACCESSED Rigidbody on Slide Object..."); }
            
            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;
            
            slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = stabilizerRadius;
            Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created ConstantForce on Slide Object");

            slideObject.AddComponent<ColliderGroup>();
            Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created ColliderGroup on Slide Object");

            Debug.Log("[Fisher-Firearms][Config-Joint-Init] Creating Config Joint and Setting Joint Values...");
            connectedJoint = item.gameObject.AddComponent<ConfigurableJoint>();
            connectedJoint.connectedBody = slideRB;
            connectedJoint.anchor = new Vector3(0, 0, -0.5f * module.slideTravelDistance);
            connectedJoint.axis = Vector3.right;
            connectedJoint.autoConfigureConnectedAnchor = false;
            connectedJoint.connectedAnchor = Vector3.zero;//new Vector3(0.04f, -0.1f, -0.22f);
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
            Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created Configurable Joint !");
            DumpRigidbodyToLog(slideRB);
        }

        protected void FixedUpdate()
        {
            //Debug.Log("[Fisher-Slide] LateUpdate slideObject position values: " + slideObject.transform.localPosition.ToString());
            if ((slideObject.transform.localPosition.z <= PULL_THRESHOLD) && !isPulledBack)
            {
                if (slideController != null)
                {
                    if (slideController.IsHeld())
                    {

                        Debug.Log("[Fisher-Firearms] Entered PulledBack position");
                        Debug.Log("[Fisher-Slide] PULL_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                        if (pullbackSound != null) pullbackSound.Play();
                        isPulledBack = true;
                        isRacked = false;
                        playSoundOnNext = true;
                        if (!roundChambered)
                        {
                            if (CountAmmoFromMagazine() > 0)
                            {
                                chamberRoundOnNext = true;
                            }
                        }
                        else
                        {
                            FirearmFunctions.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce);
                        }
                        slideController.ChamberRoundVisible(false);
                    }
                }

            }
            if ((slideObject.transform.localPosition.z > (PULL_THRESHOLD - RACK_THRESHOLD)) && isPulledBack)
            {
                Debug.Log("[Fisher-Firearms] Showing Ammo...");
                if (CountAmmoFromMagazine() > 0) { slideController.ChamberRoundVisible(true); Debug.Log("[Fisher-Firearms] Round Visible!"); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {

                Debug.Log("[Fisher-Firearms] Entered Rack position");
                Debug.Log("[Fisher-Slide] RACK_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                isRacked = true;
                isPulledBack = false;
                if (playSoundOnNext)
                {
                    if (rackforwardSound != null) rackforwardSound.Play();
                    playSoundOnNext = false;
                }

                if (chamberRoundOnNext)
                {
                    if (!ConsumeOneFromMagazine()) return;
                    slideController.ChamberRoundVisible(true);
                    chamberRoundOnNext = false;
                    roundChambered = true;
                    //return;
                }
                //return;
            }
        }

        protected void LateUpdate()
        {
            if (slideController != null) slideController.FixCustomComponents();
            else return;
            if (slideController.initialCheck) return;
            try
            {
                if (gunGripHeldRight || gunGripHeldLeft)
                {
                    slideController.UnlockSlide();
                    slideController.initialCheck = true;
                    Debug.Log("[Fisher-Firearms] Initial Check unlocks slide.");
                    Debug.Log("[Fisher-Slide] inital slideObject position values: " + slideObject.transform.localPosition.ToString());
                }
            }
            catch { Debug.Log("[Fisher-Firearms] Slide EXCEPTION"); }
        }

        public void SetFiringFlag(bool status)
        {
            isFiring = status;
        }

        public void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            // Trigger Action
            if (handle.Equals(gunGrip))
            {
                if (action == Interactable.Action.UseStart)
                {
                    // Begin Firing
                    triggerPressed = true;
                    if (!isFiring) StartCoroutine(FirearmFunctions.GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag));
                }
                if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
                {
                    // End Firing
                    triggerPressed = false;
                }
            }

            if (handle.Equals(slideHandle))
            {
                if (action == Interactable.Action.Ungrab)
                {
                    Debug.Log("[Fisher-Firearms] Slide Ungrab!");
                    slideController.SetHeld(false);
                }
            }

            // "Spell-Menu" Action
            if (action == Interactable.Action.AlternateUseStart)
            {
                Debug.Log("[Fisher-Slide] slideObject position values: " + slideObject.transform.localPosition.ToString());
                if (SlideToggleLock()) return;
                if (insertedMagazine == null || CountAmmoFromMagazine() > 0)
                {
                    // if weapon has no magazine, or a magazine with ammo, cycle fire mode
                    if (module.allowCycleFireMode)
                    {
                        if (emptySound != null) emptySound.Play();
                        fireModeSelection = FirearmFunctions.CycleFireMode(fireModeSelection, allowedFireModes);
                        SetFireSelectionAnimator(Animations, fireModeSelection);
                    }
                }
                if (CountAmmoFromMagazine() <= 0) MagazineRelease();
            }
        }

        public void OnFirearmSnapped(ObjectHolder holder)
        {
            try {
                slideObject.SetActive(false);
                slideCenterPosition.SetActive(false);
                slideForce.enabled = false;

                connectedJoint.connectedBody = item.rb;
                connectedJoint.anchor = Vector3.zero;
                connectedJoint.axis = Vector3.right;
                connectedJoint.autoConfigureConnectedAnchor = false;
                connectedJoint.connectedAnchor = Vector3.zero;
                connectedJoint.secondaryAxis = Vector3.up;
                connectedJoint.xMotion = ConfigurableJointMotion.Locked;
                connectedJoint.yMotion = ConfigurableJointMotion.Locked;
                connectedJoint.zMotion = ConfigurableJointMotion.Locked;
                connectedJoint.angularXMotion = ConfigurableJointMotion.Locked;
                connectedJoint.angularYMotion = ConfigurableJointMotion.Locked;
                connectedJoint.angularZMotion = ConfigurableJointMotion.Locked;
                connectedJoint.linearLimit = new SoftJointLimit { limit = 0.0f, bounciness = 0.0f, contactDistance = 0.0f };
                connectedJoint.massScale = 1.0f;
                connectedJoint.connectedMassScale = module.slideMassOffset;
            }
            catch { }
        }

        public void OnFirearmUnSnapped(ObjectHolder holder)
        {
            try
            {
                slideObject.SetActive(true);
                slideCenterPosition.SetActive(true);
                slideForce.enabled = true;
                connectedJoint.connectedBody = slideRB;
                connectedJoint.anchor = new Vector3(0, 0, -0.5f * module.slideTravelDistance);
                connectedJoint.axis = Vector3.right;
                connectedJoint.autoConfigureConnectedAnchor = false;
                connectedJoint.connectedAnchor = Vector3.zero;
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
            catch { }
        }
        
        public void OnAnyHandleGrabbed(Handle handle, Interactor interactor)
        {
            if (handle.Equals(gunGrip))
            {
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

                if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
            }
            if (handle.Equals(slideHandle))
            {
                Debug.Log("[Fisher-Firearms] Slide Grabbed!");
                slideController.SetHeld(true);
                if (slideController.IsLocked())
                {
                    //slideHandle.Release();
                    SlideToggleLock();
                    //slideController.ForwardState();
                }
                DumpRigidbodyToLog(slideController.rb);
            }

        }

        public void OnAnyHandleUngrabbed(Handle handle, Interactor interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
                if ((!gunGripHeldRight && !gunGripHeldLeft) && (slideController != null)) slideController.LockSlide();
            }
            if (handle.Equals(slideHandle))
            {
                slideController.SetHeld(false);
                Debug.Log("[Fisher-Firearms] Slide Ungrabbed!");
                slideController.DumpRB();
            }
        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                insertedMagazine = interactiveObject.GetComponent<ItemMagazine>();
                if (insertedMagazine != null)
                {
                    insertedMagazine.Insert();
                    if (insertedMagazine.GetMagazineID() != module.acceptedMagazineID)
                    {
                        // Reject the Magazine with incorrect ID
                        magazineHolder.UnSnap(interactiveObject);
                        insertedMagazine = null;
                        return;
                    }
                    // Update ammoCount and determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    return;
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
                Debug.LogError("[Fisher-Firearms][ERROR] Exception in Adding magazine: " + e.ToString());
            }
        }

        protected void OnMagazineRemoved(Item interactiveObject)
        {
            try
            {
                //ItemMagazine removedMagazine = interactiveObject.GetComponent<ItemMagazine>();
                if (insertedMagazine != null)
                {
                    insertedMagazine.Eject();
                    //CountAmmoFromMagazine();
                    magazineHolder.data.disableTouch = false;
                    insertedMagazine = null;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Fisher-Firearms][ERROR] Exception in removing magazine from pistol." + e.ToString());
            }
        }

        public void PreFireEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (fireSound != null) fireSound.Play();
            if (muzzleSmoke != null) muzzleSmoke.Play();
        }

        public bool Fire()
        {
            PreFireEffects();
            FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, 1.0f, false, slideCapsuleStabilizer);
            FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce);
            FirearmFunctions.ApplyRecoil(item.rb, null, 1.0f, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
            return true;
        }

        protected bool TrackedFire()
        {
            if (slideController != null)
            {
                if (slideController.IsLocked())
                {
                    return false;
                }
            }
            int currentAmmo = CountAmmoFromMagazine();
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
            else slideController.LastShot();

            return true;
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public bool SlideToggleLock()
        {
            if (slideController != null)
            {
                
                // If the slide is locked back and there is a loaded magazine inserted, load the next round
                if (slideController.IsLocked())
                {
                    if (CountAmmoFromMagazine() <= 0) return false;
                    if (ConsumeOneFromMagazine()) {
                        roundChambered = true;
                        slideController.ChamberRoundVisible(true);
                    }
                    slideController.ForwardState();
                    if (rackforwardSound != null) rackforwardSound.Play();
                    return true;
                }
                // If the slide is held back by the player and not yet locked, lock it
                else if (slideController.IsHeld() && isPulledBack)
                {
                    slideController.LockedBackState();
                    if (emptySound !=null ) emptySound.Play();
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public void MagazineRelease()
        {
            try { magazineHolder.UnSnapOne(); }
            catch { Debug.LogWarning("[Fisher-Firearms] Unable to Eject the Magazine!"); }
        }

        public int CountAmmoFromMagazine()
        {
            if (insertedMagazine != null)
            {
                return insertedMagazine.GetAmmoCount();
            }
            else return 0;
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

    }
}
