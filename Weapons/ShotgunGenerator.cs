using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Weapons
{
    class ShotgunGenerator : MonoBehaviour
    {
        /// ThunderRoad Object References ///
        protected Item item;
        protected Shared.FirearmModule module;
        protected ObjectHolder shellReceiver;
        //protected ItemMagazine insertedMagazine;
        private Light attachedLight;

        /// Trigger-Zone parameters ///
        private float PULL_THRESHOLD;
        private float RACK_THRESHOLD;
        private SphereCollider slideCapsuleStabilizer;

        /// Slide Interaction ///
        protected Handle slideHandle;
        //private SemiAutoSlide slideController;
        private Shared.ChildRigidbodyController slideController;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        private ConstantForce slideForce;
        private Rigidbody slideRB;

        /// Unity Object References ///
        public ConfigurableJoint connectedJoint;
        protected Handle gunGrip;
        protected Transform muzzlePoint;
        protected Transform rayCastPoint;
        protected Transform shellEjectionPoint;
        protected ParticleSystem muzzleFlash;
        protected ParticleSystem muzzleSmoke;
        protected AudioSource fireSound;
        protected AudioSource emptySound;
        protected AudioSource reloadSound;
        protected AudioSource pullbackSound;
        protected AudioSource rackforwardSound;
        private AudioSource shellInsertSound;
        protected Animator Animations;

        /// General Mechanics ///
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        public bool slideGripHeldRight;
        public bool slideGripHeldLeft;
        public bool isFiring;
        private bool triggerPressed = false;
        private bool isRacked = true;
        private bool isPulledBack = false;
        private bool chamberRoundOnNext = false;
        private bool roundChambered = false;
        private bool roundSpent = false;
        private bool playSoundOnNext = false;
        /// FireMode Selection and Ammo Tracking //
        private int currentReceiverAmmo;
        private bool currentSlideState = false;

        private float num1;
        private float num2;
        private float num3;
        private float num4;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();

            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.rayCastPointRef)) rayCastPoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.definition.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.definition.GetCustomReference(module.animationRef).GetComponent<Animator>();
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.definition.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.definition.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.definition.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.definition.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.shellInsertSoundRef)) shellInsertSound = item.definition.GetCustomReference(module.shellInsertSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.definition.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.definition.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.definition.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.definition.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[Fisher-Firearms][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();

            if (!String.IsNullOrEmpty(module.flashlightRef)) attachedLight = item.definition.GetCustomReference(module.flashlightRef).GetComponent<Light>();

            RACK_THRESHOLD = -0.1f * module.slideTravelDistance; //module.slideRackThreshold;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;

            currentReceiverAmmo = module.maxReceiverAmmo;

            /// Item Events ///
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;

            //item.OnSnapEvent += OnFirearmSnapped;
            //item.OnUnSnapEvent += OnFirearmUnSnapped;

            shellReceiver = item.GetComponentInChildren<ObjectHolder>();
            shellReceiver.Snapped += new ObjectHolder.HolderDelegate(this.OnShellInserted);
            //shellReceiver.UnSnapped += new ObjectHolder.HolderDelegate(this.OnShellRemoved);

        }

        protected void Start()
        {
            /// 1) Create and Initialize configurable joint between the base and slide
            /// 2) Create and Initialize the slide controller object
            /// 3) Setup the slide controller into the default state
            /// 4) Spawn and Snap in the inital magazine
            /// 5) (optional) Set the firemode selection switch to the correct position

            num1 = slideHandle.data.positionDamperMultiplier;
            num2 = slideHandle.data.positionSpringMultiplier;
            num3 = slideHandle.data.rotationDamperMultiplier;
            num4 = slideHandle.data.rotationSpringMultiplier;
            InitializeConfigurableJoint(module.slideStabilizerRadius);

            slideController = new Shared.ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);

            if (slideController == null) Debug.LogError("[Fisher-Firearms] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();

            shellReceiver.data.disableTouch = true;
            //Debug.Log("[Fisher-Firearms] Setting the Slide Interactable to Limited...");
            //LimitSlideInteraction(currentSlideState);

            return;
        }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null)
            {
                // TODO: Figure out why adding RB from code doesnt work
                slideRB = slideObject.AddComponent<Rigidbody>();
                //Debug.LogWarning("[Fisher-Firearms][Config-Joint-Init] CREATED Rigidbody ON SlideObject...");

            }
            //else
            //{
            //    Debug.Log("[Fisher-Firearms][Config-Joint-Init] ACCESSED Rigidbody on Slide Object...");
            //}

            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

            slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = stabilizerRadius;
            // Place the Stabilizer on an empty layer and then ignore collision with all player/body locomotion layers
            slideCapsuleStabilizer.gameObject.layer = 21;
            Physics.IgnoreLayerCollision(21, 12);
            Physics.IgnoreLayerCollision(21, 15);
            Physics.IgnoreLayerCollision(21, 22);
            Physics.IgnoreLayerCollision(21, 23);
            //Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            //Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created ConstantForce on Slide Object");

            //slideObject.AddComponent<ColliderGroup>();
            //Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created ColliderGroup on Slide Object");

            //Debug.Log("[Fisher-Firearms][Config-Joint-Init] Creating Config Joint and Setting Joint Values...");
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
            //Debug.Log("[Fisher-Firearms][Config-Joint-Init] Created Configurable Joint !");
            //DumpRigidbodyToLog(slideRB);
        }

        public void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (attachedLight != null) attachedLight.enabled = !attachedLight.enabled;
                if (emptySound != null) emptySound.Play();
                Debug.Log("[GreatJourney] Toggled Light!");
            }
            // Trigger Action
            if (handle.name.Equals(slideHandle.name))
            {
                if (action == Interactable.Action.Ungrab)
                {
                    // Debug.Log("[Fisher-Firearms] Slide Ungrabbed!");
                    if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                    if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                    slideController.SetHeld(false);
                    DumpRigidbodyToLog(slideController.rb);
                }
                if (action == Interactable.Action.Grab)
                {
                    //Debug.Log("[Fisher-Firearms] Slide Grabbed!");
                    if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                    slideController.SetHeld(true);
                    DumpRigidbodyToLog(slideController.rb);
                }
            }

            if (handle.Equals(gunGrip))
            {
                if (action == Interactable.Action.Grab)
                {
                    if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
                }

                if (action == Interactable.Action.Ungrab)
                {
                    if (slideController != null) slideController.LockSlide();
                    try { slideHandle.Release(); }
                    catch { }
                }

                if (action == Interactable.Action.UseStart)
                {
                    // Begin Firing
                    triggerPressed = true;
                    slideController.LockSlide();
                    if (!TrackedFire())
                    {
                        if (emptySound != null)
                        {
                            emptySound.Play();
                        }
                    }

                    //if (!isFiring) StartCoroutine(FirearmFunctions.GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag));
                }
                if (action == Interactable.Action.UseStop)
                {
                    // End Firing
                    triggerPressed = false;
                    slideController.UnlockSlide();
                }
                //"Spell-Menu" Action
                //if (action == Interactable.Action.AlternateUseStart)
                //{

                //}
            }
        }

        public void OnAnyHandleGrabbed(Handle handle, Interactor interactor)
        {
            //Debug.Log("[Fisher-Firearms] Grab: " + handle.name);
            if (handle.Equals(gunGrip))
            {
                //Debug.Log("[Fisher-Firearms] GunGrip Grabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
                if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null))
                {
                    slideHandle.data.positionDamperMultiplier = num1;
                    slideHandle.data.positionSpringMultiplier = num2;
                    slideHandle.data.rotationDamperMultiplier = num3;
                    slideHandle.data.rotationSpringMultiplier = num4;
                    slideController.UnlockSlide();
                }
                //slideHandle.data.positionDamperMultiplier = num1;
                //slideHandle.data.positionSpringMultiplier = num2;
                //slideHandle.data.rotationDamperMultiplier = num3;
                //slideHandle.data.rotationSpringMultiplier = num4;
                //if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
            }
            if (handle.name.Equals(slideHandle.name))
            {
                //Debug.Log("[Fisher-Firearms] Slide Grabbed!");
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                slideController.SetHeld(true);
                //if (slideController.IsLocked())
                //{
                //    //slideHandle.Release();
                //    SlideToggleLock();
                //    slideController.ForwardState();
                //}
                // DumpRigidbodyToLog(slideController.rb);
            }

        }

        public void OnAnyHandleUngrabbed(Handle handle, Interactor interactor, bool throwing)
        {
            Debug.Log("[Fisher-Firearms] Ungrab: " + handle.name);
            if (handle.Equals(gunGrip))
            {
                // Debug.Log("[Fisher-Firearms] GunGrip Ungrabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
                if ((!gunGripHeldRight && !gunGripHeldLeft) && (slideController != null))
                {
                    slideHandle.data.positionDamperMultiplier = 1.0f;
                    slideHandle.data.positionSpringMultiplier = 1.0f;
                    slideHandle.data.rotationDamperMultiplier = 1.0f;
                    slideHandle.data.rotationSpringMultiplier = 1.0f;
                    slideController.LockSlide();
                }
            }
            if (handle.name.Equals(slideHandle.name))
            {
                // Debug.Log("[Fisher-Firearms] Slide Ungrabbed!");
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                slideController.SetHeld(false);
                // DumpRigidbodyToLog(slideController.rb);
            }
        }

        protected void OnShellInserted(Item interactiveObject)
        {
            try
            {
                Items.InteractiveAmmo insertedShell = interactiveObject.GetComponent<Items.InteractiveAmmo>();
                shellReceiver.UnSnap(interactiveObject);
                if (insertedShell != null)
                {
                    if (insertedShell.GetAmmoType().Equals(AmmoType.ShotgunShell))
                    {
                        if (shellInsertSound != null) shellInsertSound.Play();
                        chamberRoundOnNext = true;
                        currentReceiverAmmo += 1;
                        interactiveObject.Despawn();
                        //if (currentReceiverAmmo >= module.maxReceiverAmmo) { shellReceiver.data.locked = true; }
                    }
                }

            }

            catch (Exception e)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] Exception in Adding magazine: " + e.ToString());
            }
        }

        //protected void OnShellRemoved(Item interactiveObject)
        //{
        //    try
        //    {
        //        Common.InteractiveAmmo insertedShell = interactiveObject.GetComponent<Common.InteractiveAmmo>();
        //        if (insertedShell != null)
        //        { }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log("[Fisher-Firearms][ERROR] Exception in removing magazine from pistol." + e.ToString());
        //    }
        //}

        public void PreFireEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (fireSound != null) fireSound.Play();
            if (muzzleSmoke != null) muzzleSmoke.Play();
        }

        public bool Fire()
        {
            PreFireEffects();
            FirearmFunctions.ShotgunBlast(item, module.projectileID, rayCastPoint, module.blastRange, module.blastForce, module.bulletForce, FirearmFunctions.GetItemSpellChargeID(item), module.throwMult, false, slideCapsuleStabilizer);
            //FirearmFunctions.ShootProjectile(item, module.projectileID, rayCastPoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, 1.0f, false, slideCapsuleStabilizer, true);
            //FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce);
            FirearmFunctions.ApplyRecoil(item.rb, module.recoilForces, 1.0f, gunGripHeldLeft || slideGripHeldLeft, gunGripHeldRight || slideGripHeldRight, module.hapticForce);
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
            if (!roundChambered || roundSpent) return false;

            Fire();
            roundSpent = true;

            return true;
        }

        public void SetFiringFlag(bool status) { isFiring = status; }

        public bool TriggerIsPressed() { return triggerPressed; }

        public int CountAmmoFromReceiver()
        {
            return currentReceiverAmmo;
        }

        public bool ConsumeOneFromReceiver()
        {
            if (currentReceiverAmmo > 0)
            {
                currentReceiverAmmo -= 1;
                return true;
            }
            else return false;
        }

        protected void LateUpdate()
        {

            //Debug.Log("[Fisher-Slide] LateUpdate slideObject position values: " + slideObject.transform.localPosition.ToString());
            if ((slideObject.transform.localPosition.z <= PULL_THRESHOLD) && !isPulledBack)
            {
                if (slideController != null)
                {
                    if (slideController.IsHeld())
                    {
                        //Debug.Log("[Fisher-Firearms] Entered PulledBack position");
                        currentSlideState = false;
                        //Debug.Log("[Fisher-Slide] PULL_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                        if (pullbackSound != null) pullbackSound.Play();
                        isPulledBack = true;
                        isRacked = false;
                        playSoundOnNext = true;
                        slideController.LockedBackState();
                        FirearmFunctions.Animate(Animations, module.openAnimationRef);
                        shellReceiver.data.disableTouch = false;
                        //if (shellReceiver.data.locked && (currentReceiverAmmo < module.maxReceiverAmmo))
                        //{
                        //    shellReceiver.data.locked = false;
                        //}

                        if (roundChambered)
                        {
                            if (roundSpent) { FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer); }
                            else { FirearmFunctions.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer); }
                            roundChambered = false;
                            slideController.ChamberRoundVisible(false);
                        }
                        if (CountAmmoFromReceiver() > 0)
                        {
                            chamberRoundOnNext = true;
                        }

                    }
                }

            }
            if ((slideObject.transform.localPosition.z > (PULL_THRESHOLD - RACK_THRESHOLD)) && isPulledBack)
            {
                //Debug.Log("[Fisher-Firearms] Showing Ammo...");
                if (CountAmmoFromReceiver() > 0) { slideController.ChamberRoundVisible(true); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {
                //Debug.Log("[Fisher-Firearms] Entered Rack position");
                currentSlideState = true;
                //Debug.Log("[Fisher-Slide] RACK_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
                isRacked = true;
                isPulledBack = false;
                shellReceiver.data.disableTouch = true;
                slideController.ForwardState();
                FirearmFunctions.Animate(Animations, module.closeAnimationRef);
                if (playSoundOnNext)
                {
                    if (rackforwardSound != null) rackforwardSound.Play();
                    playSoundOnNext = false;
                }

                if (chamberRoundOnNext)
                {
                    if (!ConsumeOneFromReceiver()) return;
                    slideController.ChamberRoundVisible(true);
                    chamberRoundOnNext = false;
                    roundChambered = true;
                    roundSpent = false;
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
                    // Debug.Log("[Fisher-Firearms] Initial Check unlocks slide.");
                    //Debug.Log("[Fisher-Slide] inital slideObject position values: " + slideObject.transform.localPosition.ToString());
                }
            }
            catch { Debug.Log("[Fisher-Firearms] Slide EXCEPTION"); }
        }
    }
}
