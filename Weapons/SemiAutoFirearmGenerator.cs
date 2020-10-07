using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.SemiAuto
{
    public class SemiAutoFirearmGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;

        /// Ammo Display Controller ///
        private TextureProcessor ammoCounter;
        private MeshRenderer ammoCounterMesh;
        private Texture2D digitsGridTexture;
        /// Magazine Parameters///
        protected ObjectHolder magazineHolder;
        protected ItemMagazine insertedMagazine;
        private Item currentInteractiveObject;
        /// Trigger-Zone parameters ///
        private float PULL_THRESHOLD;
        private float RACK_THRESHOLD;
        private SphereCollider slideCapsuleStabilizer;
        /// Slide Interaction ///
        protected Handle slideHandle;
        private Shared.ChildRigidbodyController slideController;
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

        void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();

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
            else Debug.LogError("[Fisher-GreatJourney][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.definition.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[Fisher-GreatJourney][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.definition.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[Fisher-GreatJourney][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();

            if (!String.IsNullOrEmpty(module.ammoCounterRef))
            {
                Debug.Log("[Fisher-GreatJourney] Getting Ammo Counter Objects ...");
                ammoCounterMesh = item.definition.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
                digitsGridTexture = (Texture2D)item.definition.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
                Debug.Log("[Fisher-GreatJourney] GOT Ammo Counter Objects !!!");
            }

            RACK_THRESHOLD = -0.1f * module.slideTravelDistance;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;

            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);

            if (module.allowedFireModes != null)
            {
                allowedFireModes = new List<int>(module.allowedFireModes);
            }

            if (digitsGridTexture == null) Debug.LogError("[Fisher-GreatJourney] COULD NOT GET GRID TEXTURE");
            if (ammoCounterMesh == null) Debug.LogError("[Fisher-GreatJourney] COULD NOT GET MESH RENDERER");

            if ((digitsGridTexture != null) && (ammoCounterMesh != null))
            {
                ammoCounter = new TextureProcessor();
                ammoCounter.SetGridTexture(digitsGridTexture);
                ammoCounter.SetTargetRenderer(ammoCounterMesh);
                Debug.Log("[Fisher-GreatJourney] Sucessfully Setup Ammo Counter!!");
            }

            /// Item Events ///
            item.OnHeldActionEvent += OnHeldAction;

            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;

            magazineHolder = item.GetComponentInChildren<ObjectHolder>();
            magazineHolder.Snapped += new ObjectHolder.HolderDelegate(this.OnMagazineInserted);
            magazineHolder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnMagazineReleased);

        }

        void Start()
        {
            /// 1) Create and Initialize configurable joint between the base and slide
            /// 2) Create and Initialize the slide controller object
            /// 3) Setup the slide controller into the default state
            /// 4) Spawn and Snap in the inital magazine
            /// 5) (optional) Set the firemode selection switch to the correct position
            InitializeConfigurableJoint(module.slideStabilizerRadius);

            slideController = new Shared.ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);

            if (slideController == null) Debug.LogError("[Fisher-GreatJourney] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();

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

            SetFireSelectionAnimator(Animations, fireModeSelection);
            ammoCounter.DisplayUpdate(0);
        }

        protected void LateUpdate()
        {
            if (!gunGripHeldLeft && !gunGripHeldRight)
            {
                triggerPressed = false;
            }
            if ((slideObject.transform.localPosition.z <= PULL_THRESHOLD) && !isPulledBack)
            {
                if (slideController != null)
                {
                    if (slideController.IsHeld())
                    {
                        Debug.Log("[Fisher-GreatJourney] Entered PulledBack position");
                        Debug.Log("[Fisher-Slide] PULL_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                        if (pullbackSound != null) pullbackSound.Play();
                        isPulledBack = true;
                        isRacked = false;
                        playSoundOnNext = true;
                        if (!roundChambered) { chamberRoundOnNext = true; }
                        else
                        {
                            FirearmFunctions.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer);
                            roundChambered = false;
                            chamberRoundOnNext = true;
                        }
                        slideController.ChamberRoundVisible(false);
                    }
                }

            }
            if ((slideObject.transform.localPosition.z > (PULL_THRESHOLD - RACK_THRESHOLD)) && isPulledBack)
            {
                Debug.Log("[Fisher-GreatJourney] Showing Ammo...");
                if (CountAmmoFromMagazine() > 0) { slideController.ChamberRoundVisible(true); Debug.Log("[Fisher-GreatJourney] Round Visible!"); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {
                Debug.Log("[Fisher-GreatJourney] Entered Rack position");
                Debug.Log("[Fisher-Slide] RACK_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                isRacked = true;
                isPulledBack = false;
                if (chamberRoundOnNext)
                {
                    if (ConsumeOneFromMagazine())
                    {
                        slideController.ChamberRoundVisible(true);
                        chamberRoundOnNext = false;
                        roundChambered = true;
                        UpdateAmmoCounter();
                    }
                }

                if (playSoundOnNext)
                {
                    if (rackforwardSound != null) rackforwardSound.Play();
                    playSoundOnNext = false;
                }

            }

            if (slideController != null) slideController.FixCustomComponents();
            else return;
            if (slideController.initialCheck) return;
            try
            {
                if (gunGripHeldRight || gunGripHeldLeft)
                {
                    slideController.UnlockSlide();
                    slideController.initialCheck = true;
                    Debug.Log("[Fisher-GreatJourney] Initial Check unlocks slide.");
                    Debug.Log("[Fisher-Slide] inital slideObject position values: " + slideObject.transform.localPosition.ToString());
                }
            }
            catch { Debug.Log("[Fisher-GreatJourney] Slide EXCEPTION"); }
        }

        public void UpdateAmmoCounter()
        {
            if (ammoCounter == null) return;
            if (!roundChambered) { ammoCounter.DisplayUpdate(CountAmmoFromMagazine()); }
            else
            {
                ammoCounter.DisplayUpdate(CountAmmoFromMagazine() + 1);
            }
            //if (!roundChambered) { ammoCounter.DisplayUpdate(0); }
            //else
            //{
            //    ammoCounter.DisplayUpdate(CountAmmoFromMagazine() + 1);
            //}
        }

        public void SetAmmoCounter(int value)
        {
            if (ammoCounter != null) { ammoCounter.DisplayUpdate(value); }
        }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null)
            {
                // TODO: Figure out why adding RB from code doesnt work
                slideRB = slideObject.AddComponent<Rigidbody>();
                Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] CREATED Rigidbody ON SlideObject...");

            }
            else { Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] ACCESSED Rigidbody on Slide Object..."); }

            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

            slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = stabilizerRadius;
            slideCapsuleStabilizer.gameObject.layer = 21;
            Physics.IgnoreLayerCollision(21, 12);
            Physics.IgnoreLayerCollision(21, 15);
            Physics.IgnoreLayerCollision(21, 22);
            Physics.IgnoreLayerCollision(21, 23);
            Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] Created ConstantForce on Slide Object");

            Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] Creating Config Joint and Setting Joint Values...");
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
            Debug.Log("[Fisher-GreatJourney][Config-Joint-Init] Created Configurable Joint !");
            DumpRigidbodyToLog(slideRB);
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
                if (action == Interactable.Action.UseStop)
                {
                    // End Firing
                    triggerPressed = false;
                }
            }

            // "Spell-Menu" Action
            if (action == Interactable.Action.AlternateUseStart)
            {
                Debug.Log("[Fisher-Slide] Attempting Slide Lock Toggle ....  ");

                if (SlideToggleLock()) return;
                Debug.Log("[Fisher-Slide] Slide was not locked, ejecting magazine! ");
                MagazineRelease();
            }

            if (action == Interactable.Action.Ungrab)
            {
                //if (handle.Equals(gunGrip))
                //{

                //}

                if (handle.Equals(slideHandle))
                {
                    Debug.Log("[Fisher-GreatJourney] Slide Ungrabbed!");
                    if (slideController != null) slideController.SetHeld(false);
                }

            }


        }

        public void OnAnyHandleGrabbed(Handle handle, Interactor interactor)
        {
            if (handle.Equals(gunGrip))
            {
                Debug.Log("[Fisher-GreatJourney] Main Handle Grabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
            }

            if (handle.Equals(slideHandle))
            {
                Debug.Log("[Fisher-GreatJourney] Slide Grabbed!");
                slideController.SetHeld(true);
                slideController.ForwardState();
                DumpRigidbodyToLog(slideController.rb);
            }

            if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
        }

        public void OnAnyHandleUngrabbed(Handle handle, Interactor interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                Debug.Log("[Fisher-GreatJourney] Main Handle Ungrabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
            }
            if (handle.Equals(slideHandle))
            {
                Debug.Log("[Fisher-GreatJourney] Slide Ungrabbed!");
                slideController.SetHeld(false);
                DumpRigidbodyToLog(slideController.rb);
            }

            if ((!gunGripHeldRight && !gunGripHeldLeft))
            {
                triggerPressed = false;
                if (slideController != null) slideController.LockSlide();
            }
        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                insertedMagazine = interactiveObject.GetComponent<ItemMagazine>();
                currentInteractiveObject = interactiveObject;
                currentInteractiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);
                if (insertedMagazine != null)
                {
                    insertedMagazine.Insert();
                    item.IgnoreObjectCollision(interactiveObject);
                    // determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    magazineHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    if (insertedMagazine.GetMagazineID() != module.acceptedMagazineID)
                    {
                        // Reject the Magazine with incorrect ID
                        MagazineRelease();
                    }
                    //return;
                }
                else
                {
                    // Reject the non-Magazine object
                    magazineHolder.UnSnap(interactiveObject);
                    insertedMagazine = null;
                    currentInteractiveObject = null;
                }
            }

            catch (Exception e)
            {
                Debug.LogError("[Fisher-GreatJourney][ERROR] Exception in Adding magazine: " + e.ToString());
            }

            if (roundChambered) UpdateAmmoCounter();
        }

        public void MagazineRelease()
        {
            Debug.Log("[Fisher-GreatJourney] Releasing Magazine!");
            try
            {
                if (currentInteractiveObject != null)
                {
                    magazineHolder.UnSnap(currentInteractiveObject);
                    item.IgnoreObjectCollision(currentInteractiveObject);
                    currentInteractiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);

                    if (insertedMagazine != null)
                    {
                        insertedMagazine.Eject();
                        insertedMagazine = null;
                    }
                    currentInteractiveObject = null;
                }
            }
            catch { Debug.LogWarning("[Fisher-GreatJourney] Unable to Eject the Magazine!"); }

            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();
        }

        protected void OnMagazineReleased(Item interactiveObject)
        {
            UpdateAmmoCounter();
            insertedMagazine = null;
            currentInteractiveObject = null;
            magazineHolder.data.disableTouch = false;
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

        public void PreFireEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (fireSound != null) fireSound.Play();
            if (muzzleSmoke != null) muzzleSmoke.Play();
        }

        public bool Fire()
        {
            PreFireEffects();
            FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false, slideCapsuleStabilizer);
            FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer);
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
                //playSoundOnNext = true;
                slideController.LastShot();
            }

            UpdateAmmoCounter();

            return true;
        }

    }
}
