using System;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Weapons
{
    class ShotgunGenerator : MonoBehaviour
    {
        /// ThunderRoad Object References ///
        protected Item item;
        protected Shared.FirearmModule module;
        protected Holder shellReceiver;
        //protected ItemMagazine insertedMagazine;
        private Light attachedLight;
        /// Trigger-Zone parameters ///
        private float PULL_THRESHOLD;
        private float RACK_THRESHOLD;
        private SphereCollider slideCapsuleStabilizer;
        /// Slide Interaction ///
        protected Handle slideHandle;
        //private SemiAutoSlide slideController;
        private ChildRigidbodyController slideController;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        private ConstantForce slideForce;
        private Rigidbody slideRB;
        private bool holdingSlideTrigger = false;
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
        //protected AudioSource reloadSound;
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
        //private bool currentSlideState = false;
        private float num1;
        private float num2;
        private float num3;
        private float num4;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();
            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.rayCastPointRef)) rayCastPoint = item.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.GetCustomReference(module.animationRef).GetComponent<Animator>();
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.shellInsertSoundRef)) shellInsertSound = item.GetCustomReference(module.shellInsertSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            else Debug.LogError("[ModularFirearmsFramework][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[ModularFirearmsFramework][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[ModularFirearmsFramework][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();
            if (!String.IsNullOrEmpty(module.flashlightRef)) attachedLight = item.GetCustomReference(module.flashlightRef).GetComponent<Light>();
            RACK_THRESHOLD = -0.1f * module.slideTravelDistance; //module.slideRackThreshold;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;
            currentReceiverAmmo = module.maxReceiverAmmo;
            /// Item Events ///
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnAnyHandleGrabbed;
            item.OnUngrabEvent += OnAnyHandleUngrabbed;
            //item.OnTouchActionEvent += TouchActionEvent;
            //item.OnSnapEvent += OnFirearmSnapped;
            //item.OnUnSnapEvent += OnFirearmUnSnapped;
            //shellReceiver = item.GetComponentInChildren<Holder>();
            shellReceiver = item.GetCustomReference(module.shellReceiverDef).GetComponentInChildren<Holder>();
            shellReceiver.Snapped += new Holder.HolderDelegate(this.OnShellInserted);
            shellReceiver.UnSnapped += new Holder.HolderDelegate(this.OnShellRemoved);
        }

        //private void IgnoreMovingLayer()
        //{
        //    foreach (ColliderGroup colliderGroup in this.item.colliderGroups)
        //    {
        //        foreach (Collider collider in colliderGroup.colliders)
        //        {
        //            Physics.IgnoreLayerCollision(collider.gameObject.layer, GameManager.GetLayer(LayerName.MovingObject));
        //        }
        //    }
        //}

        protected void Start()
        {
            //IgnoreMovingLayer();
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
            slideController = new ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);
            if (slideController == null) Debug.LogError("[ModularFirearmsFramework] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();
            shellReceiver.data.disableTouch = true;
            //LimitSlideInteraction(currentSlideState);
            return;
        }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null)
            {
                // NOTE: RB Needs to be assigned at Runtime (doesn't work when added to prefab in Editor)
                slideRB = slideObject.AddComponent<Rigidbody>();
            }
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
            slideForce = slideObject.AddComponent<ConstantForce>();
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
            //DumpRigidbodyToLog(slideRB);
        }

        //public void TouchActionEvent(Interactable interactable, Interactable.Action action)
        //{
        //    if (action == Interactable.Action.Ungrab)
        //    {
                
        //        Debug.Log("[ModularFirearmsFramework] Ungrab: " + interactable.interactableId);
        //        if (interactable.interactableId == gunGrip.interactableId)
        //        {
        //            // Debug.Log("[ModularFirearmsFramework] GunGrip Ungrabbed!");
        //            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
        //            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
        //            if ((!gunGripHeldRight && !gunGripHeldLeft) && (slideController != null))
        //            {
        //                slideHandle.data.positionDamperMultiplier = 1.0f;
        //                slideHandle.data.positionSpringMultiplier = 1.0f;
        //                slideHandle.data.rotationDamperMultiplier = 1.0f;
        //                slideHandle.data.rotationSpringMultiplier = 1.0f;
        //                slideController.LockSlide();
        //            }
        //        }
        //        if (handle.name.Equals(slideHandle.name))
        //        {
        //            // Debug.Log("[ModularFirearmsFramework] Slide Ungrabbed!");
        //            if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
        //            if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
        //            slideController.SetHeld(false);
        //            // DumpRigidbodyToLog(slideController.rb);
        //        }
        //    }
        //}

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (attachedLight != null) {
                    attachedLight.enabled = !attachedLight.enabled;
                    if (emptySound != null) emptySound.Play();
                }
            }
            // Trigger Action
            if (handle.name.Equals(slideHandle.name))
            {
                if (((action == Interactable.Action.UseStart)|| (action == Interactable.Action.AlternateUseStart)) && (!holdingSlideTrigger))
                {
                    holdingSlideTrigger = true;
                    slideController.LockSlide(false);
                    if (emptySound != null) emptySound.Play();
                }
                if (((action == Interactable.Action.UseStop)|| (action == Interactable.Action.AlternateUseStop)) && (holdingSlideTrigger))
                {
                    holdingSlideTrigger = false;
                    slideController.UnlockSlide(false);
                    // if (emptySound != null) emptySound.Play();
                }
                if (action == Interactable.Action.Ungrab)
                {
                    if (holdingSlideTrigger) { holdingSlideTrigger = false; slideController.UnlockSlide(); }
                    if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                    if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                    slideController.SetHeld(false);
                    //DumpRigidbodyToLog(slideController.rb);
                }
                if (action == Interactable.Action.Grab)
                {
                    if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                    slideController.SetHeld(true);
                    //DumpRigidbodyToLog(slideController.rb);
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
                    if (!holdingSlideTrigger) slideController.LockSlide(false);
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
                    if (!holdingSlideTrigger) slideController.UnlockSlide(false);
                }
                //"Spell-Menu" Action
                //if (action == Interactable.Action.AlternateUseStart)
                //{
                //}
            }
        }

        public void OnAnyHandleGrabbed(Handle handle, RagdollHand interactor)
        {
            if (handle.Equals(gunGrip))
            {
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
            }
            if (handle.name.Equals(slideHandle.name))
            {
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                slideController.SetHeld(true);
            }
        }

        public void OnAnyHandleUngrabbed(Handle handle, RagdollHand interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
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
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                slideController.SetHeld(false);
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
                        //if (currentReceiverAmmo >= module.maxReceiverAmmo) { shellReceiver.data.locked = true; }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ModularFirearmsFramework][ERROR] Exception in Adding magazine: " + e.ToString());
            }
        }

        protected void OnShellRemoved(Item interactiveObject)
        {
            try
            {
                Items.InteractiveAmmo insertedShell = interactiveObject.GetComponent<Items.InteractiveAmmo>();
                if (insertedShell != null)
                {
                    if (insertedShell.GetAmmoType().Equals(AmmoType.ShotgunShell))
                    {
                        interactiveObject.Despawn();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("[ModularFirearmsFramework][ERROR] Exception in removing shell from receiver." + e.ToString());
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
            //FirearmFunctions.ShotgunBlast(item, module.projectileID, rayCastPoint, module.blastRange, module.blastForce, module.bulletForce, FirearmFunctions.GetItemSpellChargeID(item), module.throwMult, false, slideCapsuleStabilizer);
            //FirearmFunctions.ProjectileBurst(item, module.projectileID, muzzlePoint,  FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false, slideCapsuleStabilizer);
            ItemData spawnedItemData = Catalog.GetData<ItemData>(module.projectileID, true);
            String imbueSpell = GetItemSpellChargeID(item);

            var projectileData = Catalog.GetData<ItemData>(module.projectileID, true);
            if ((muzzlePoint == null) || (String.IsNullOrEmpty(module.projectileID))) return false;
            if (projectileData == null)
            {
                Debug.LogError("[ModularFirearmsFramework][ERROR] No projectile named " + module.projectileID.ToString());
                return false;
            }
            foreach (Vector3 offsetVec in buckshotOffsetPosiitions)
            {
                projectileData.SpawnAsync(i =>
                {
                    try
                    {
                        //i.transform.position = muzzlePoint.position + offsetVec;
                        //i.transform.rotation = Quaternion.Euler(muzzlePoint.rotation.eulerAngles);
                        //i.rb.velocity = this.item.rb.velocity;
                        //i.rb.AddForce(i.rb.transform.forward * 1000.0f * module.bulletForce);
                        //this.item.IgnoreObjectCollision(i);
                        //i.IgnoreObjectCollision(this.item);
                        //i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                        i.Throw(1f, Item.FlyDetection.Forced);
                        item.IgnoreObjectCollision(i);
                        i.IgnoreObjectCollision(item);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                        IgnoreProjectile(this.item, i, true);
                        i.transform.position = muzzlePoint.position + offsetVec;
                        i.transform.rotation = Quaternion.Euler(muzzlePoint.rotation.eulerAngles);
                        i.rb.velocity = item.rb.velocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * module.bulletForce);
                        if (slideCapsuleStabilizer != null)
                        {
                            try
                            {
                                i.IgnoreColliderCollision(slideCapsuleStabilizer);
                                foreach (ColliderGroup CG in this.item.colliderGroups)
                                {
                                    foreach (Collider C in CG.colliders)
                                    {
                                        Physics.IgnoreCollision(i.colliderGroups[0].colliders[0], C);
                                    }
                                }
                                // i.IgnoreColliderCollision(shooterItem.colliderGroups[0].colliders[0]);
                                //Physics.IgnoreCollision(IgnoreArg1, projectile.definition.GetCustomReference(projectileColliderReference).GetComponent<Collider>());
                            }
                            catch { }
                        }
                        Projectiles.BasicProjectile projectileController = i.gameObject.GetComponent<Projectiles.BasicProjectile>();
                        if (projectileController != null)
                        {
                            projectileController.SetShooterItem(this.item);
                        }
                        //-- Optional Switches --//
                        //i.rb.useGravity = false;
                        //i.Throw(throwMult, Item.FlyDetection.CheckAngle);
                        //i.SetColliderAndMeshLayer(GameManager.GetLayer(LayerName.Default));
                        //i.SetColliderLayer(GameManager.GetLayer(LayerName.None));
                        //i.ignoredItem = shooterItem;
                        //shooterItem.IgnoreObjectCollision(i);
                        //Physics.IgnoreLayerCollision(GameManager.GetLayer(LayerName.None), GameManager.GetLayer(LayerName.Default));
                        if (!String.IsNullOrEmpty(imbueSpell))
                        {
                            // Set imbue charge on projectile using ItemProjectileSimple subclass
                            //Projectiles.SimpleProjectile projectileController = i.gameObject.GetComponent<Projectiles.SimpleProjectile>();
                            if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("[ModularFirearmsFramework] EXCEPTION IN SPAWNING " + ex.Message + " \n " + ex.StackTrace);
                    }
                },
                Vector3.zero,
                Quaternion.Euler(Vector3.zero),
                null,
                false);
            }
            //FirearmFunctions.ShootProjectile(item, module.projectileID, rayCastPoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, 1.0f, false, slideCapsuleStabilizer, true);
            //FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce);
            FrameworkCore.ApplyRecoil(item.rb, module.recoilForces, 1.0f, gunGripHeldLeft || slideGripHeldLeft, gunGripHeldRight || slideGripHeldRight, module.hapticForce);
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
            if ((slideObject.transform.localPosition.z <= PULL_THRESHOLD) && !isPulledBack)
            {
                if (slideController != null)
                {
                    if (slideController.IsHeld())
                    {
                        //currentSlideState = false;
                        if (pullbackSound != null) pullbackSound.Play();
                        isPulledBack = true;
                        isRacked = false;
                        playSoundOnNext = true;
                        slideController.LockedBackState();
                        FrameworkCore.Animate(Animations, module.openAnimationRef);
                        shellReceiver.data.disableTouch = false;
                        //if (shellReceiver.data.locked && (currentReceiverAmmo < module.maxReceiverAmmo))
                        //{
                        //    shellReceiver.data.locked = false;
                        //}
                        if (roundChambered)
                        {
                            if (roundSpent) { FrameworkCore.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer); }
                            else { FrameworkCore.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer); }
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
                if (CountAmmoFromReceiver() > 0) { slideController.ChamberRoundVisible(true); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {
                //currentSlideState = true;
                isRacked = true;
                isPulledBack = false;
                shellReceiver.data.disableTouch = true;
                slideController.ForwardState();
                FrameworkCore.Animate(Animations, module.closeAnimationRef);
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
                }
            }
            catch { Debug.Log("[ModularFirearmsFramework] Slide EXCEPTION"); }
        }
    }
}
