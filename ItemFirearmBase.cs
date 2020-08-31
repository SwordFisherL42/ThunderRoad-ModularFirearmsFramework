using System;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms
{
    public class ItemFirearmBase : MonoBehaviour
    {
        //ThunderRoad Object References
        protected Item item;
        protected Item startMagazine;
        protected ItemModuleFirearmBase module;
        protected Handle gunGrip;
        protected ItemSlide childSlide;
        protected ItemMagazine insertedMagazine;
        protected ObjectHolder pistolGripHolder;
        //Unity Object References
        protected ConfigurableJoint slideJoint;
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
        public bool gunGripHeldLeft;
        public bool gunGripHeldRight;
        protected bool rightHapticFlag = false;
        protected bool leftHapticFlag = false;
        protected bool isRacked = true;
        protected bool isPulledBack;
        protected bool waitingForChamber;
        protected bool roundChambered;
        protected bool playSoundOnNext;
        protected bool triggerPressed;
        //FireMode Selection and Ammo Tracking
        private FireMode fireModeSelection;
        private int counter;
        protected int ammoCount;
        public bool isFiring;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            item.OnHeldActionEvent += OnHeldAction;

            module = item.data.GetModule<ItemModuleFirearmBase>();

            if (!String.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);
            if (!String.IsNullOrEmpty(module.shellEjectionRef)) shellEjectionPoint = item.definition.GetCustomReference(module.shellEjectionRef);
            if (!String.IsNullOrEmpty(module.soundNames[0])) fireSound = item.definition.GetCustomReference(module.soundNames[0]).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.soundNames[1])) emptySound = item.definition.GetCustomReference(module.soundNames[1]).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.soundNames[2])) pullbackSound = item.definition.GetCustomReference(module.soundNames[2]).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.soundNames[3])) rackforwardSound = item.definition.GetCustomReference(module.soundNames[3]).GetComponent<AudioSource>();
            if (!String.IsNullOrEmpty(module.flashRef)) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (!String.IsNullOrEmpty(module.mainHandleRef)) gunGrip = item.definition.GetCustomReference(module.mainHandleRef).GetComponent<Handle>();
            if (!String.IsNullOrEmpty(module.animationRef)) Animations = item.definition.GetCustomReference(module.animationRef).GetComponent<Animator>();

            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }

            pistolGripHolder = item.GetComponentInChildren<ObjectHolder>();
            pistolGripHolder.Snapped += new ObjectHolder.HolderDelegate(this.OnMagazineInserted);
            pistolGripHolder.UnSnapped += new ObjectHolder.HolderDelegate(this.OnMagazineRemoved);

            fireModeSelection = (FireMode)FirearmFunctions.fireModeEnums.GetValue(module.fireMode);

        }

        protected void Start()
        {
            // Spawn and Snap in the inital magazine
            var magazineData = Catalog.GetData<ItemPhysic>(module.acceptedMagazineID, true);
            if (magazineData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No Magazine named " + module.acceptedMagazineID.ToString());
                return;
            }
            else
            {
                startMagazine = magazineData.Spawn(true);
                pistolGripHolder.Snap(startMagazine);
            }
            pistolGripHolder.data.disableTouch = !module.allowGrabMagazineFromGun;

            childSlide = item.definition.GetCustomReference(module.childSlideRef).GetComponent<ItemSlide>();
            if (childSlide == null) Debug.LogError("[Fisher-Firearms] ERROR! CHILD SLIDE WAS NULL");
            SetFireSelectionAnimator(Animations, fireModeSelection);
            return;
        }

        protected void OnDestroy()
        {
            if (childSlide != null) childSlide.DestoryThisSlide();
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

            // "Spell-Menu" Action
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (SlideToggleLock()) return;
                else if (CountAmmoFromMagazine() <= 0) MagazineRelease();
                else
                {
                    // If the weapon is loaded, cycle fire mode for alternate start
                    if (module.allowCycleFireMode)
                    {
                        if (emptySound != null) emptySound.Play();
                        fireModeSelection = FirearmFunctions.CycleFireMode(fireModeSelection);
                        SetFireSelectionAnimator(Animations, fireModeSelection);
                    }
                    else
                    {
                        MagazineRelease();
                    }
                }
            }
        }

        public void OnMainGripGrabbed(Interactor interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

            if ((gunGripHeldRight || gunGripHeldLeft) && (childSlide != null)) childSlide.UnlockSlide();

        }

        public void OnMainGripUnGrabbed(Interactor interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
            if ((!gunGripHeldRight && !gunGripHeldLeft) && (childSlide != null)) childSlide.LockSlide();
        }

        protected void OnMagazineInserted(Item interactiveObject)
        {
            try
            {
                ItemMagazine addedMagazine = interactiveObject.GetComponent<ItemMagazine>();
                if (addedMagazine != null)
                {
                    addedMagazine.Insert();
                    if (addedMagazine.GetMagazineID() != module.acceptedMagazineID)
                    {
                        // Reject the Magazine with incorrect ID
                        pistolGripHolder.UnSnap(interactiveObject);
                        return;
                    }
                    // Update ammoCount and determine if the magazine can be pulled from the weapon (otherwise the ejection button is required)
                    ammoCount = addedMagazine.GetAmmoCount();
                    pistolGripHolder.data.disableTouch = !module.allowGrabMagazineFromGun;
                    return;
                }
                else
                {
                    // Reject the non-Magazine object
                    pistolGripHolder.UnSnap(interactiveObject);
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
                ItemMagazine removedMagazine = interactiveObject.GetComponent<ItemMagazine>();
                if (removedMagazine != null)
                {
                    removedMagazine.Eject();
                    CountAmmoFromMagazine();
                    pistolGripHolder.data.disableTouch = false;
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[Fisher-Firearms][ERROR] Exception in removing magazine from pistol." + e.ToString());
            }
        }

        protected void OnTriggerEnter(Collider hit)
        {
            // State-Machine logic for slide mechanics //
            if (!hit.isTrigger) return;
            ItemSlide triggerSlide = hit.GetComponentInParent<ItemSlide>();
            if (triggerSlide != null)
            {
                //Debug.Log("[Fisher-Firearms] Entered on ItemSlide component " + hit.name);
                if (hit.name.Contains(module.rackTriggerRef))
                {
                    //Debug.Log("[Fisher-Firearms] Entered Rack position");
                    isRacked = true;
                    isPulledBack = false;
                    if (playSoundOnNext)
                    {
                        rackforwardSound.Play();
                        playSoundOnNext = false;
                    }
                    if (waitingForChamber)
                    {
                        ConsumeOneFromMagazine();
                        childSlide.ChamberRoundVisible(true);
                        waitingForChamber = false;
                        //Debug.Log("[Fisher-Firearms] Chamber Round Visible!");
                        return;
                    }
                    return;
                }

                if (!triggerSlide.IsHeld()) return;
                if (triggerSlide.IsLocked()) return;

                if (hit.name.Contains(module.slideTriggerRef))
                {
                    if (!playSoundOnNext) pullbackSound.Play();
                    playSoundOnNext = true;
                    isPulledBack = true;
                    if (!roundChambered && CountAmmoFromMagazine() <= 0) return;
                    if (!isRacked) return;
                    //Debug.Log("[Fisher-Firearms] Entered PulledBack position");
                    if (roundChambered) UnChamberRound(false);
                    ChamberOneRound();
                    isRacked = false;
                }
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
            ammoCount = CountAmmoFromMagazine();
            if (ammoCount <= 0 || !roundChambered) return false;
            else if (ammoCount == 1) childSlide.LastShot();
            else childSlide.BlowBack();
            // Round cycle sequence
            UnChamberRound();
            Fire();
            ChamberOneRound();
            return true;
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public bool SlideToggleLock()
        {
            if (childSlide != null)
            {
                // If the slide is locked back and there is a loaded magazine inserted, load the next round
                if (childSlide.IsLocked() && (CountAmmoFromMagazine() > 0))
                {
                    ChamberOneRound();
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
            }
            return false;
        }

        public void MagazineRelease()
        {
            try { pistolGripHolder.UnSnapOne(); }
            catch { Debug.LogWarning("[Fisher-Firearms] Unable to Eject the Magazine!"); }
        }

        public int CountAmmoFromMagazine()
        {
            counter = 0;
            if (pistolGripHolder.holdObjects.Count > 0)
            {
                insertedMagazine = pistolGripHolder.holdObjects[0].GetComponent<ItemMagazine>();
                if (insertedMagazine != null)
                {
                    counter = insertedMagazine.GetAmmoCount();
                }
            }
            if (roundChambered) counter += 1;
            return counter;
        }

        public void ConsumeOneFromMagazine()
        {
            if (pistolGripHolder.holdObjects.Count > 0) insertedMagazine = pistolGripHolder.holdObjects[0].GetComponent<ItemMagazine>();
            else return;
            if (insertedMagazine != null) insertedMagazine.ConsumeOne();
        }

        public void ChamberOneRound()
        {
            if (roundChambered || CountAmmoFromMagazine() <= 0) return;
            roundChambered = true;
            waitingForChamber = true;
            return;
        }

        public void UnChamberRound(bool roundIsEmpty = true)
        {
            roundChambered = false;
            childSlide.ChamberRoundVisible(roundChambered);
            if (roundIsEmpty) FirearmFunctions.ShootProjectile(item, module.shellID, shellEjectionPoint, null, module.shellEjectionForce); //SpawnShellCasing(module.shellID);
            else FirearmFunctions.ShootProjectile(item, module.ammoID, shellEjectionPoint, null, module.shellEjectionForce); ;
        }

    }
}
