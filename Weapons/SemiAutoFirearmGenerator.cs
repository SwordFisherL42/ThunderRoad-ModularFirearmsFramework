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
    public class SemiAutoFirearmGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;

        private delegate void DelegatedActionFunction();
        private DelegatedActionFunction spellImmediateAction;
        private DelegatedActionFunction spellDelayedAction;

        public bool projectileIsSpawning = false;

        private LayerMask laserIgnore;
        private float lastSpellMenuPress;
        private GameObject compass;
        private Transform rayCastPoint;
        private int currentIndex;
        private int compassIndex;

        private LineRenderer attachedLaser;
        private Transform laserStart;
        private Transform laserEnd;
        private float maxLaserDistance;

        private Light attachedLight;
        private Material flashlightMaterial;
        private Color flashlightEmissionColor;

        private Handle foreGrip;
        /// Ammo Display Controller ///
        private Shared.TextureProcessor ammoCounter;
        private MeshRenderer ammoCounterMesh;
        private Texture2D digitsGridTexture;
        /// Magazine Parameters///
        protected Holder magazineHolder;
        protected Items.InteractiveMagazine insertedMagazine;
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

        protected AudioSource fireSound1;
        protected AudioSource fireSound2;
        protected AudioSource fireSound3;
        private int soundCounter;
        private int maxSoundCounter;

        protected AudioSource emptySound;
        protected AudioSource reloadSound;
        protected AudioSource pullbackSound;
        protected AudioSource rackforwardSound;
        protected Animator Animations;
        /// General Mechanics ///
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        public bool slideGripHeldLeft;
        public bool slideGripHeldRight;
        public bool isFiring;
        private bool triggerPressed = false;
        private bool spellMenuPressed = false;
        private bool isRacked = true;
        private bool isPulledBack = false;
        private bool chamberRoundOnNext = false;
        private bool roundChambered = false;
        private bool playSoundOnNext = false;
        /// FireMode Selection and Ammo Tracking //
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;

        public bool ProjectileIsSpawning()
        {
            return projectileIsSpawning;
        }

        public void SetProjectileSpawningState(bool newState)
        {
            projectileIsSpawning = newState;
        }

        void Awake()
        {
            soundCounter = 0;
            maxSoundCounter = 0;

            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();

            /// Set all Object References ///
            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.GetCustomReference(module.animationRef).GetComponent<Animator>();
            if (!String.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();

            if (!String.IsNullOrEmpty(module.fireSound1Ref)) { fireSound1 = item.GetCustomReference(module.fireSound1Ref).GetComponent<AudioSource>(); maxSoundCounter++; soundCounter = 1; }
            if (!String.IsNullOrEmpty(module.fireSound2Ref)) { fireSound2 = item.GetCustomReference(module.fireSound2Ref).GetComponent<AudioSource>(); maxSoundCounter++; }
            if (!String.IsNullOrEmpty(module.fireSound3Ref)) { fireSound3 = item.GetCustomReference(module.fireSound3Ref).GetComponent<AudioSource>(); maxSoundCounter++; }


            if (!String.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.pullSoundRef)) pullbackSound = item.GetCustomReference(module.pullSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.rackSoundRef)) rackforwardSound = item.GetCustomReference(module.rackSoundRef).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.smokeRef)) muzzleSmoke = item.GetCustomReference(module.smokeRef).GetComponent<ParticleSystem>();

            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();

            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Main Handle (\"mainHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideHandleRef)) slideObject = item.GetCustomReference(module.slideHandleRef).gameObject;
            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Slide Handle (\"slideHandleRef\") in JSON! Weapon will not work as intended !!!");
            if (!String.IsNullOrEmpty(module.slideCenterRef)) slideCenterPosition = item.GetCustomReference(module.slideCenterRef).gameObject;
            else Debug.LogError("[Fisher-ModularFirearms][ERROR] No Reference to Slide Center Position(\"slideCenterRef\") in JSON! Weapon will not work as intended...");
            if (slideObject != null) slideHandle = slideObject.GetComponent<Handle>();

            if (!String.IsNullOrEmpty(module.compassRef)) compass = item.GetCustomReference(module.compassRef).gameObject;

            if (!String.IsNullOrEmpty(module.flashlightRef))
            {
                attachedLight = item.GetCustomReference(module.flashlightRef).GetComponent<Light>();

                if (!String.IsNullOrEmpty(module.flashlightMeshRef))
                {
                    flashlightMaterial = item.GetCustomReference(module.flashlightMeshRef).GetComponent<MeshRenderer>().material;
                    flashlightEmissionColor = flashlightMaterial.GetColor("_EmissionColor");
                    if (!attachedLight.enabled) flashlightMaterial.SetColor("_EmissionColor", Color.black);
                }

            }

            if (!String.IsNullOrEmpty(module.laserRef)) attachedLaser = item.GetCustomReference(module.laserRef).GetComponent<LineRenderer>();

            if (compass != null)
            {
                currentIndex = -1;
                compassIndex = 0;
            }

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

            if (!String.IsNullOrEmpty(module.foregripHandleRef)) foreGrip = item.GetCustomReference(module.foregripHandleRef).GetComponent<Handle>();

            if (!String.IsNullOrEmpty(module.ammoCounterRef))
            {
                //Debug.Log("[Fisher-ModularFirearms] Getting Ammo Counter Objects ...");
                ammoCounterMesh = item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
                digitsGridTexture = (Texture2D)item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
                //Debug.Log("[Fisher-ModularFirearms] GOT Ammo Counter Objects !!!");
            }

            lastSpellMenuPress = 0.0f;

            if (module.laserTogglePriority)
            {
                spellImmediateAction = ToggleLaser;
                spellDelayedAction = MagazineRelease;
            }
            else
            {
                spellImmediateAction = MagazineRelease;
                spellDelayedAction = ToggleLaser;
            }

            RACK_THRESHOLD = -0.1f * module.slideTravelDistance;
            PULL_THRESHOLD = -0.5f * module.slideTravelDistance;

            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);

            if (module.allowedFireModes != null)
            {
                allowedFireModes = new List<int>(module.allowedFireModes);
            }

            //if (digitsGridTexture == null) Debug.LogError("[Fisher-ModularFirearms] COULD NOT GET GRID TEXTURE");
            //if (ammoCounterMesh == null) Debug.LogError("[Fisher-ModularFirearms] COULD NOT GET MESH RENDERER");

            if ((digitsGridTexture != null) && (ammoCounterMesh != null))
            {
                ammoCounter = new Shared.TextureProcessor();
                ammoCounter.SetGridTexture(digitsGridTexture);
                ammoCounter.SetTargetRenderer(ammoCounterMesh);
                //Debug.Log("[Fisher-ModularFirearms] Sucessfully Setup Ammo Counter!!");
            }

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

            slideController = new Shared.ChildRigidbodyController(item, module);
            slideController.InitializeSlide(slideObject);

            if (slideController == null) Debug.LogError("[Fisher-ModularFirearms] ERROR! CHILD SLIDE CONTROLLER WAS NULL");
            else slideController.SetupSlide();

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

            SetFireSelectionAnimator(Animations, fireModeSelection);
            if (ammoCounter != null) ammoCounter.DisplayUpdate(0);
        }

        protected void LateUpdate()
        {

            if (!gunGripHeldLeft && !gunGripHeldRight)
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
                        //Debug.Log("[Fisher-ModularFirearms] Entered PulledBack position");
                        //Debug.Log("[Fisher-Slide] PULL_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
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
                //Debug.Log("[Fisher-ModularFirearms] Showing Ammo...");
                if (CountAmmoFromMagazine() > 0) { slideController.ChamberRoundVisible(true); }
            }
            if ((slideObject.transform.localPosition.z >= RACK_THRESHOLD) && !isRacked)
            {
                //Debug.Log("[Fisher-ModularFirearms] Entered Rack position");
                //Debug.Log("[Fisher-Slide] RACK_THRESHOLD slideObject position values: " + slideObject.transform.localPosition.ToString());
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

            UpdateLaserPoint();
            UpdateCompassPosition();

            if (slideController != null) slideController.FixCustomComponents();
            else return;
            if (slideController.initialCheck) return;
            try
            {
                if (gunGripHeldRight || gunGripHeldLeft)
                {
                    slideController.UnlockSlide();
                    slideController.initialCheck = true;
                    //Debug.Log("[Fisher-ModularFirearms] Initial Check unlocks slide.");
                    //Debug.Log("[Fisher-Slide] inital slideObject position values: " + slideObject.transform.localPosition.ToString());
                }
            }
            catch { Debug.Log("[Fisher-ModularFirearms] Slide EXCEPTION"); }
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

        public void UpdateCompassPosition()
        {
            if (compass == null) return;

            compassIndex = (int)Mathf.Floor(compass.transform.rotation.eulerAngles.y / 45.0f);
            if (currentIndex != compassIndex)
            {
                //Debug.Log("Compass Y Rotation: " + compass.transform.rotation.eulerAngles.y);
                //Debug.Log("compassIndex: " + compassIndex);
                currentIndex = compassIndex;
                //compass.transform.rotation = Quaternion.Euler(compassIndex * 45.0f, 0.0f, 0.0f);
                compass.transform.Rotate(0, 0, -1.0f * compass.transform.rotation.eulerAngles.z, Space.Self);
                compass.transform.Rotate(0, 0, compassIndex * 45.0f, Space.Self);
                //Debug.Log("Compass Z Rotation: " + compass.transform.rotation.eulerAngles.z);
            }
        }

        public void UpdateAmmoCounter()
        {
            if (ammoCounter == null) return;
            if (!roundChambered) { ammoCounter.DisplayUpdate(CountAmmoFromMagazine()); }
            else { ammoCounter.DisplayUpdate(CountAmmoFromMagazine() + 1); }
            // if (!roundChambered) { ammoCounter.DisplayUpdate(0); }
            // else{ ammoCounter.DisplayUpdate(CountAmmoFromMagazine() + 1); }
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
                //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] CREATED Rigidbody ON SlideObject...");

            }
            //else { Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] ACCESSED Rigidbody on Slide Object..."); }

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
            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created ConstantForce on Slide Object");

            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Creating Config Joint and Setting Joint Values...");
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
            // Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created Configurable Joint !");
            //DumpRigidbodyToLog(slideRB);
        }

        private void ToggleLight()
        {
            if (emptySound != null) emptySound.Play();

            if (attachedLight != null)
            {
                attachedLight.enabled = !attachedLight.enabled;
                if (flashlightMaterial != null)
                {
                    if (attachedLight.enabled) flashlightMaterial.SetColor("_EmissionColor", flashlightEmissionColor);
                    else flashlightMaterial.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private void ToggleLaser()
        {
            if (module.allowCycleFireMode)
            {
                if (emptySound != null) emptySound.Play();
                fireModeSelection = FirearmFunctions.CycleFireMode(fireModeSelection, allowedFireModes);
               SetFireSelectionAnimator(Animations, fireModeSelection);
            }
            if (attachedLaser == null) return;
            if (emptySound != null) emptySound.Play();
            attachedLaser.enabled = !attachedLaser.enabled;

        }

        private IEnumerator SpellMenuAction()
        {
            bool timeTriggeredAction = false;

            while (spellMenuPressed)
            {
                if ((Time.time - lastSpellMenuPress) >= module.laserToggleHoldTime)
                {
                    timeTriggeredAction = true;
                    spellDelayedAction();
                    break;
                }
                yield return new WaitForEndOfFrame();
            }

            if (!timeTriggeredAction)
            {
                spellImmediateAction();
            }

            spellMenuPressed = false;

            yield return null;
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (foreGrip != null)
            {
                if (handle.Equals(foreGrip))
                {
                    if ((action == Interactable.Action.UseStart) || (action == Interactable.Action.AlternateUseStart)) ToggleLight();
                }
            }


            if (handle.Equals(gunGrip))
            {
                // Trigger Action
                if (action == Interactable.Action.UseStart)
                {
                    // Begin Firing
                    triggerPressed = true;
                    if (!isFiring) StartCoroutine(FirearmFunctions.GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag, ProjectileIsSpawning));
                }
                if (action == Interactable.Action.UseStop)
                {
                    // End Firing
                    triggerPressed = false;
                }

                // "Spell-Menu" Action
                if (action == Interactable.Action.AlternateUseStart)
                {
                    if (SlideToggleLock()) return;
                    lastSpellMenuPress = Time.time;
                    spellMenuPressed = true;
                    StartCoroutine(SpellMenuAction());
                }

                if (action == Interactable.Action.AlternateUseStop)
                {
                    spellMenuPressed = false;
                }

            }

            if (action == Interactable.Action.Grab)
            {
                if (handle.Equals(gunGrip))
                {
                    if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

                    //ForceDrop();
                    if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
                }

                if (handle.Equals(slideHandle))
                {
                    if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                    if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                    //    Debug.Log("[Fisher-ModularFirearms] Slide Ungrabbed!");
                    if (slideController != null) slideController.SetHeld(true);
                    slideController.ForwardState();
                }

            }

            if (action == Interactable.Action.Ungrab)
            {

                if (handle.Equals(gunGrip))
                {
                    if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                    if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;

                    //ForceDrop();
                    if (!gunGripHeldRight && !gunGripHeldLeft)
                    {
                        if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                        if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                        if (((slideController != null))) { slideController.LockSlide(); }
                        ForceDrop();
                    }
                }

                if (handle.Equals(slideHandle))
                {
                    //    Debug.Log("[Fisher-ModularFirearms] Slide Ungrabbed!");
                    if (slideController != null) slideController.SetHeld(false);
                }

            }


        }

        public void ForceDrop()
        {
            try { slideHandle.Release(); }
            catch { }
            if (slideController != null) slideController.LockSlide();
        }

        public void OnAnyHandleGrabbed(Handle handle, RagdollHand interactor)
        {
            if (handle.Equals(gunGrip))
            {
                //     Debug.Log("[Fisher-ModularFirearms] Main Handle Grabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
                //if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) { slideController.UnlockSlide(); slideController.ForwardState(); }
                if ((gunGripHeldRight || gunGripHeldLeft) && (slideController != null)) slideController.UnlockSlide();
            }

            if (handle.Equals(slideHandle))
            {
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = true;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = true;
                //    Debug.Log("[Fisher-ModularFirearms] Slide Grabbed!");
                slideController.SetHeld(true);
                slideController.ForwardState();
                //DumpRigidbodyToLog(slideController.rb);
            }


        }

        public void OnAnyHandleUngrabbed(Handle handle, RagdollHand interactor, bool throwing)
        {
            if (handle.Equals(gunGrip))
            {
                //    Debug.Log("[Fisher-ModularFirearms] Main Handle Ungrabbed!");
                if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
                if (!gunGripHeldRight && !gunGripHeldLeft)
                {
                    if (((slideController != null))) { slideController.LockSlide(); }
                    ForceDrop();
                }

            }
            if (handle.Equals(slideHandle))
            {
                if (interactor.playerHand == Player.local.handRight) slideGripHeldRight = false;
                if (interactor.playerHand == Player.local.handLeft) slideGripHeldLeft = false;
                //    Debug.Log("[Fisher-ModularFirearms] Slide Ungrabbed!");
                slideController.SetHeld(false);
                //DumpRigidbodyToLog(slideController.rb);
            }

            //if ((!gunGripHeldRight && !gunGripHeldLeft))
            //{
            //    triggerPressed = false;
            //    if (fireModeSelection.Equals(FireMode.Auto))
            //    {
            //        if ((fireSoundLoop != null) && (fireSoundOut != null))
            //        {
            //            StartCoroutine(EndContiniousFiringSound(fireSoundLoop, fireSoundOut, false));
            //        }
            //    }
            //    if (slideController != null) slideController.LockSlide();
            //}
        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                interactiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);
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

            if (roundChambered) UpdateAmmoCounter();
        }

        protected void OnMagazineRemoved(Item interactiveObject)
        {
            try
            {
                if (insertedMagazine != null)
                {
                    //insertedMagazine.Eject(item.colliderGroups.ToArray());
                    insertedMagazine.Eject(item);
                    insertedMagazine = null;
                }
                //currentInteractiveObject = null;
            }
            catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }

            magazineHolder.data.disableTouch = false;
            UpdateAmmoCounter();

            //try
            //{
            //    if (insertedMagazine != null)
            //    {
            //        insertedMagazine.Remove();
            //        insertedMagazine = null;
            //    }
            //    //currentInteractiveObject = null;
            //}
            //catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }

            //magazineHolder.data.disableTouch = false;
            //UpdateAmmoCounter();
        }

        public void MagazineRelease()
        {
            //  Debug.Log("[Fisher-ModularFirearms] Releasing Magazine!");
            try
            {
                if (magazineHolder.holdObjects.Count > 0)
                {
                    magazineHolder.UnSnap(magazineHolder.holdObjects[0]);
                }

            }
            catch { }


        }

        //public void MagazineRelease()
        //{
        //    Debug.Log("[Fisher-ModularFirearms] Releasing Magazine!");
        //    try
        //    {
        //        if (currentInteractiveObject != null)
        //        {
        //            magazineHolder.UnSnap(currentInteractiveObject);
        //            item.IgnoreObjectCollision(currentInteractiveObject);
        //            currentInteractiveObject.IgnoreColliderCollision(slideCapsuleStabilizer);

        //            if (insertedMagazine != null)
        //            {
        //                insertedMagazine.Eject();
        //                insertedMagazine = null;
        //            }
        //            currentInteractiveObject = null;
        //        }
        //    }
        //    catch { Debug.LogWarning("[Fisher-ModularFirearms] Unable to Eject the Magazine!"); }

        //    magazineHolder.data.disableTouch = false;
        //    UpdateAmmoCounter();
        //}

        //protected void OnMagazineReleased(Item interactiveObject)
        //{
        //    UpdateAmmoCounter();
        //    insertedMagazine = null;
        //    //currentInteractiveObject = null;
        //    //magazineHolder.data.disableTouch = false;
        //}

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

            if (!module.useBuckshot) FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false, slideCapsuleStabilizer, SetProjectileSpawningState);
            else FirearmFunctions.ProjectileBurst(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult, false, slideCapsuleStabilizer);

            FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce, 1.0f, false, slideCapsuleStabilizer, SetProjectileSpawningState);
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
