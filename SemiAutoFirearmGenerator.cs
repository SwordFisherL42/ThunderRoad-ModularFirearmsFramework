using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms
{
    public class SemiAutoFirearmGenerator : MonoBehaviour
    {
        private readonly float CC_RADIUS = 0.02f;
        private readonly float CC_HEIGHT_TX = 0.05f;
        private readonly float CC_HEIGHT_RX = 0.075f;
        private CapsuleCollider RearTrigger;
        //private readonly float CC_TRIG_OFFSET = 0.015f;
        public ConfigurableJoint connectedJoint;
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        public bool isFiring;

        private ChildSlide childSlide;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        protected Handle gunGrip;
        protected Handle slideHandle;
        private ConstantForce slideForce;

        //ThunderRoad Object References
        protected Item item;
        protected SemiAutoModule module;

        private ItemMagazine insertedMagazine;
        protected ObjectHolder pistolGripHolder;
        private Rigidbody slideRB;

        //Unity Object References
        protected Transform muzzlePoint;
        protected Transform shellEjectionPoint;
        protected ParticleSystem muzzleFlash;
        protected AudioSource fireSound;
        protected AudioSource emptySound;
        protected AudioSource reloadSound;
        protected AudioSource pullbackSound;
        protected AudioSource rackforwardSound;
        protected Animator Animations;
        //Interaction settings

        protected bool rightHapticFlag = false;
        protected bool leftHapticFlag = false;
        protected bool isRacked = true;
        protected bool isPulledBack = false;
        protected bool chamberRoundOnNext = false;
        protected bool roundChambered = false;
        protected bool playSoundOnNext = false;
        protected bool triggerPressed;

        //FireMode Selection and Ammo Tracking
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<SemiAutoModule>();

            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.definition.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.definition.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.definition.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.definition.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.definition.GetCustomReference(module.animationRef).GetComponent<Animator>();

            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.definition.GetCustomReference(module.slideHandleRef).gameObject;
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.definition.GetCustomReference(module.slideCenterRef).gameObject;
            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);
            if (module.allowedFireModes != null)
            {
                allowedFireModes = new List<int>(module.allowedFireModes);
            }

            // Item Events
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;
            item.OnSnapEvent += OnFirearmSnapped;
            item.OnUnSnapEvent += OnFirearmUnSnapped;
            pistolGripHolder = item.GetComponentInChildren<ObjectHolder>();
            pistolGripHolder.Snapped += new ObjectHolder.HolderDelegate(this.OnMagazineInserted);
            pistolGripHolder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnMagazineRemoved);


        }


        protected void Start()
        {
            // Create configurable joint between the base RB and ChildSlide RB
            InitializeConfigurableJoint();

            childSlide = new ChildSlide(item, module);
            childSlide.InitializeSlide(slideObject);

            // Create the zone which determines the slide position
            CreateTriggerCollider();
            // Spawn and Snap in the inital magazine
            var magazineData = Catalog.GetData<ItemPhysic>(module.acceptedMagazineID, true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No Magazine named " + module.acceptedMagazineID.ToString());
                return;
            }
            else
            {
                Item startMagazine = magazineData.Spawn(true);
                pistolGripHolder.Snap(startMagazine);
            }
            pistolGripHolder.data.disableTouch = !module.allowGrabMagazineFromGun;

            if (childSlide == null) Debug.LogError("[Fisher-Firearms] ERROR! CHILD SLIDE WAS NULL");
            else childSlide.SetupSlide();

            SetFireSelectionAnimator(Animations, fireModeSelection);
            return;
        }
        
        private void CreateTriggerCollider()
        {
            Debug.Log("[Fisher-Firearms] Creating Triggers Colliders...");
            GameObject RearTriggerObj = new GameObject("RearTrigger");
            RearTriggerObj.transform.parent = item.gameObject.transform;
            RearTriggerObj.transform.position = new Vector3(slideHandle.definition.touchCenter.x + slideHandle.gameObject.transform.position.x,
                slideHandle.definition.touchCenter.y + slideHandle.gameObject.transform.position.y,
                slideHandle.definition.touchCenter.z + slideHandle.gameObject.transform.position.z - slideHandle.definition.touchRadius - module.slideTravelDistance);
            RearTrigger = RearTriggerObj.AddComponent<CapsuleCollider>();
            RearTrigger.center = Vector3.zero;
            RearTrigger.isTrigger = true;
            RearTrigger.radius = CC_RADIUS;
            RearTrigger.height = CC_HEIGHT_RX;
            RearTrigger.direction = 0;
            Debug.Log("[Fisher-Firearms] Triggers Colliders COMPLETE!");
        }

        private void InitializeConfigurableJoint()
        {
            // TODO: Figure out why adding RB from code doesnt work
            //Rigidbody slideRB;
            //slideRB = slideObject.GetComponent<Rigidbody>();
            //if (slideRB == null)
            //{
            //    slideRB = slideObject.AddComponent<Rigidbody>();
            //    Debug.Log("CREATED RIGIDBODY ON SlideObject...");
            //}

            slideRB = slideObject.GetComponent<Rigidbody>();
            Debug.Log("[Fisher-Firearms] Accessed RIGIDBODY on Slide Object...");
            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;
            
            SphereCollider slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = 0.02f;
            Debug.Log("[Fisher-Firearms] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            Debug.Log("[Fisher-Firearms] Created ConstantForce on Slide Object");

            slideObject.AddComponent<ColliderGroup>();
            Debug.Log("[Fisher-Firearms] Created ColliderGroup on Slide Object");

            Debug.Log("[Fisher-Firearms] Creating Config Joint and Setting Joint Values...");
            connectedJoint = item.gameObject.AddComponent<ConfigurableJoint>();
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
            Debug.Log("[Fisher-Firearms] Created Configurable Joint ...");
        }

        protected void LateUpdate()
        {
            if (childSlide != null) childSlide.FixCustomComponents();
            else return;
            if (childSlide.initialCheck) return;
            try
            {
                if (gunGripHeldRight || gunGripHeldLeft)
                {
                    childSlide.UnlockSlide();
                    childSlide.initialCheck = true;
                    Debug.Log("[Fisher-Firearms] Initial Check unlocks slide.");
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
                    childSlide.SetHeld(false);
                }
            }

            // "Spell-Menu" Action
            if (action == Interactable.Action.AlternateUseStart)
            {
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

                if ((gunGripHeldRight || gunGripHeldLeft) && (childSlide != null)) childSlide.UnlockSlide();
            }
            if (handle.Equals(slideHandle))
            {
                Debug.Log("[Fisher-Firearms] Slide Grabbed!");
                childSlide.SetHeld(true);
                if (childSlide.IsLocked())
                {
                    childSlide.ForwardState();
                }
                childSlide.DumpRB();
            }

        }

        public void OnAnyHandleUngrabbed(Handle handle, Interactor interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
                if ((!gunGripHeldRight && !gunGripHeldLeft) && (childSlide != null)) childSlide.LockSlide();
            }
            if (handle.Equals(slideHandle))
            {
                childSlide.SetHeld(false);
                Debug.Log("[Fisher-Firearms] Slide Ungrabbed!");
                childSlide.DumpRB();
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
                        pistolGripHolder.UnSnap(interactiveObject);
                        insertedMagazine = null;
                        return;
                    }
                    // Update ammoCount and determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    pistolGripHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    return;
                }
                else
                {
                    // Reject the non-Magazine object
                    pistolGripHolder.UnSnap(interactiveObject);
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
                    pistolGripHolder.data.disableTouch = false;
                    insertedMagazine = null;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Fisher-Firearms][ERROR] Exception in removing magazine from pistol." + e.ToString());
            }
        }

        protected void OnTriggerExit(Collider hit)
        {
            //// State-Machine logic for slide mechanics //
            
            if (!hit.isTrigger) return;
            if (!childSlide.IsHeld()) { Debug.Log("[Fisher-Firearms] CHILD SLIDE NOT HELD "); return; }
            else if (hit.name.Contains("SlideObject"))
            {
                Debug.Log("[Fisher-Firearms] Entered PulledBack position");
                pullbackSound.Play();
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
                
            }
            
        }

        protected void OnTriggerEnter(Collider hit)
        {
            if (!hit.isTrigger) return;
            if (hit.name.Contains("SlideObject"))
            {
                Debug.Log("[Fisher-Firearms] Entered Rack position");
                isRacked = true;
                isPulledBack = false;
                if (playSoundOnNext)
                {
                    rackforwardSound.Play();
                    playSoundOnNext = false;
                }
                
                if (chamberRoundOnNext)
                {
                    if (!ConsumeOneFromMagazine()) return;
                    childSlide.ChamberRoundVisible(true);
                    chamberRoundOnNext = false;
                    roundChambered = true;
                    return;
                }
                return;
            }
        }

        public void PreFireEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (fireSound != null) fireSound.Play();
        }

        public bool Fire()
        {
            PreFireEffects();
            FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce);
            FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce);
            FirearmFunctions.ApplyRecoil(item.rb, null, 1.0f, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
            return true;
        }

        protected bool TrackedFire()
        {
            if (childSlide != null)
            {
                if (childSlide.IsLocked())
                {
                    return false;
                }
            }
            int currentAmmo = CountAmmoFromMagazine();
            if (!roundChambered) return false;
            else 
            // Round cycle sequence
            roundChambered = false;
            childSlide.ChamberRoundVisible(roundChambered);
            Fire();
            if (ConsumeOneFromMagazine())
            {
                roundChambered = true;
                childSlide.ChamberRoundVisible(roundChambered);
                childSlide.BlowBack();
            }
            else childSlide.LastShot();

            return true;
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public bool SlideToggleLock()
        {
            if (childSlide != null)
            {
                // If the slide is locked back and there is a loaded magazine inserted, load the next round
                if (childSlide.IsLocked() && CountAmmoFromMagazine() > 0)
                {
                    if (ConsumeOneFromMagazine()) roundChambered = true;
                    childSlide.ForwardState();
                    rackforwardSound.Play();
                    return true;
                }
                // If the slide is held back by the player and not yet locked, lock it
                else if (childSlide.IsHeld() && isPulledBack && !childSlide.IsLocked())
                {
                    childSlide.LockedBackState();
                    emptySound.Play();
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public void MagazineRelease()
        {
            try { pistolGripHolder.UnSnapOne(); }
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
