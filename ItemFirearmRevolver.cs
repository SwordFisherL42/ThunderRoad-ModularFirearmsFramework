using UnityEngine;
using System.Collections;
using BS;
using System.Linq;

namespace FishersFirearmsModular
{
    // Modular revolver base class, handling all extended interactions with other classes.
    public class ItemFirearmRevolver : MonoBehaviour
    {
        // Item vars
        protected Item item;
        protected ItemModuleFirearmRevolver module;
        protected Rigidbody Rb;
        protected ParticleSystem muzzleFlash;
        protected ParticleSystem smoke;
        protected AudioSource shotSound;
        protected AudioSource emptySound;
        protected AudioSource readySound;
        protected AudioSource reloadSound;
        protected Transform muzzlePoint;
        protected Animator animation;
        protected bool rightHapticFlag = false;
        protected bool leftHapticFlag = false;
        protected float lastVelocity = 0f;
        protected float prevAnimSpeed;

        // Quiver/Ammo vars
        protected ItemQuiver itemQuiver;
        protected ItemModuleQuiver moduleQuiver;
        protected ObjectHolder holder;
        protected bool isOpen;
        protected bool bulletVisible;
        protected int bulletIndex;

        // Projectile vars
        protected Item projectile;
        protected Rigidbody projectileBody;

        // Interaction vars
        protected Handle gunGrip;
        protected bool gunGripHeldLeft;
        protected bool gunGripHeldRight;

        protected void Awake()
        {
            // Basic item references
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleFirearmRevolver>();
            Rb = item.GetComponent<Rigidbody>();
            if (module.gunGripID != null) gunGrip = item.definition.GetCustomReference(module.gunGripID).GetComponent<Handle>();

            // Spawning positions
            if (module.muzzlePositionRef != null) muzzlePoint = item.definition.GetCustomReference(module.muzzlePositionRef);

            // Sounds
            if (module.soundNames[0] != null) readySound = item.definition.GetCustomReference(module.soundsRef).Find(module.soundNames[0]).GetComponent<AudioSource>();
            if (module.soundNames[1] != null) shotSound = item.definition.GetCustomReference(module.soundsRef).Find(module.soundNames[1]).GetComponent<AudioSource>();
            if (module.soundNames[2] != null) emptySound = item.definition.GetCustomReference(module.soundsRef).Find(module.soundNames[2]).GetComponent<AudioSource>();
            if (module.soundNames[3] != null) reloadSound = item.definition.GetCustomReference(module.soundsRef).Find(module.soundNames[3]).GetComponent<AudioSource>();

            // Particles systems
            if (module.flashRef != null) muzzleFlash = item.definition.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
            if (module.smokeRef != null && module.muzzlePositionRef != null) smoke = item.definition.GetCustomReference(module.muzzlePositionRef).Find(module.smokeRef).GetComponent<ParticleSystem>();

            // Animations
            if (module.animatorRef != null)
            {
                animation = item.definition.GetCustomReference(module.animatorRef).GetComponent<Animator>();
                if (module.animationSpeed > 0) animation.speed = animation.speed * module.animationSpeed;
                prevAnimSpeed = animation.speed;
            }

            // Quiver (Chamber) definitions
            itemQuiver = this.GetComponent<ItemQuiver>();
            moduleQuiver = item.data.GetModule<ItemModuleQuiver>();
            holder = itemQuiver.GetComponentInChildren<ObjectHolder>();
            holder.Snapped += new ObjectHolder.HolderDelegate(this.OnProjectileAdded);
            itemQuiver.holder.data.disableTouch = true;
            isOpen = false;

            // Setup item events
            item.OnHeldActionEvent += OnHeldAction;
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnGripGrabbed;
                gunGrip.UnGrabbed += OnGripUnGrabbed;
            }
        }

        protected void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            // Release all shells if chamber is open, otherwise attempt to fire the weapon.
            // Alt-Use (spell button) toggles the chamber open/close state.
            if (action == Interactable.Action.UseStart)
            {
                if (isOpen && (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name == module.idleOpenAnim)) foreach (Item chamberItem in itemQuiver.holder.holdObjects.ToList()) itemQuiver.holder.UnSnap(chamberItem);
                else if (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.idleAnimPrefix)) TrackedFire();
            }
            
            if (action == Interactable.Action.AlternateUseStart)
            {
                ToggleChamber();
            }
        }

        // Check for flip-to-close gesture on FixedUpdate
        private void FixedUpdate()
        {
            if (!module.gestureEnabled) return;

            if ((Mathf.Abs(Rb.velocity.x - lastVelocity) > module.minGestureVelocity) && (isOpen))
            {
                prevAnimSpeed = animation.speed;
                animation.speed = animation.speed * 2;
                CloseChamber();
            }
            lastVelocity = Rb.velocity.x;
        }

        // Modular Ammo Functions
        public void OpenChamber() {
            isOpen = true;
            itemQuiver.holder.data.disableTouch = false;
            prevAnimSpeed = animation.speed;
            animation.speed = animation.speed * 2;
            Animate(module.openingAnim);
            animation.speed = prevAnimSpeed;
        }

        public void CloseChamber()
        {
            itemQuiver.holder.data.disableTouch = true;
            isOpen = false;
            StartCoroutine(SpinToRandom());
        }

        public void ToggleChamber()
        {
            if (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.fireAnimPrefix)) return;
            if ((!isOpen) && (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.idleAnimPrefix))) OpenChamber();
            else if (isOpen && (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name == module.idleOpenAnim)) CloseChamber();
        }


        protected void OnProjectileAdded(Item interactiveObject)
        {
            try {
                ItemAmmoLoader addedLoader = interactiveObject.GetComponent<ItemAmmoLoader>();
                if (addedLoader!=null)
                {
                    itemQuiver.holder.UnSnap(interactiveObject);
                    interactiveObject.Despawn();
                    int loaderCount = addedLoader.CountBullets();
                    if (itemQuiver.holder.definition.slots.Count == loaderCount)
                    {
                        itemQuiver.SpawnAllProjectiles();
                    }
                    else
                    {
                        for (int i = 0; i < loaderCount; i++)
                        {
                            itemQuiver.SpawnProjectile();
                        }
                    }
                    return;
                }

                ItemAmmo insertedAmmo = interactiveObject.GetComponent<ItemAmmo>();
                if (insertedAmmo == null)
                {
                    itemQuiver.holder.UnSnap(interactiveObject);
                    interactiveObject.Despawn();
                }
                else if (insertedAmmo.GetAmmoType() != module.ammoType)
                {
                    itemQuiver.holder.UnSnap(interactiveObject);
                }
            }
            catch { Debug.Log("[F-L42][ERROR] Exception in Adding Projectile."); }
        }


        // Method for Player Firing the weapon. Tracks bullet position/state, and performs the associated actions for each state
        protected void TrackedFire()
        {   
            ItemAmmo firedAmmo = null;
            // If you are firing on a position that has no bullet or shell, move to next position but still animate the chamber/play the `empty sound`.
            if (bulletIndex <= itemQuiver.holder.holdObjects.Count - 1) firedAmmo = itemQuiver.holder.holdObjects[bulletIndex].GetComponent<ItemAmmo>();
            else
            {
                StartCoroutine(AnimateAndFire(bulletIndex, true));
                bulletIndex++;
                if (bulletIndex >= itemQuiver.holder.definition.slots.Count) bulletIndex = 0;
                return;
            }

            // If you are firing on a position that has an `ItemAmmo` item, check if the bullet is loaded or spent.
            if (firedAmmo != null)
            {
                if (firedAmmo.isLoaded)
                {
                    StartCoroutine(AnimateAndFire(bulletIndex, false));
                    firedAmmo.Consume();
                    bulletIndex++;
                }
                else
                {
                    StartCoroutine(AnimateAndFire(bulletIndex, true));
                    bulletIndex++;
                }

                if (bulletIndex >= itemQuiver.holder.definition.slots.Count) bulletIndex = 0;
            }
            else Debug.Log("[F-L42][ERROR] Logical Exception, ammo was somehow NULL...");

        }

        // Helper Functions //
        public bool Fire()
        {
            PreFireEffects();
            SpawnProjectile(module.projectileID);
            ApplyRecoil();
            return true;
        }

        public void Animate(string animationName)
        {
            if ((animation == null) || ((animationName == null) || (animationName == ""))) return;
            animation.Play(animationName);
        }

        // Coroutine Definitions //
        
        // Play the animation to spin the revolver and fire a projectile
        // Depending on module settings, the bullet is either fired after a delay or after the animation is completed.
        private IEnumerator AnimateAndFire(int spindex, bool misfire = false)
        {
            Animate(module.fireAnimPrefix + spindex);
            if (!module.lockFiringToAnimation)
            {
                yield return new WaitForSeconds(module.firingDelay);
                if (!misfire) Fire();
                else emptySound.Play();
            }
            do yield return null;
            while (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.fireAnimPrefix));
            if (module.lockFiringToAnimation)
            {
                if (!misfire) Fire();
                else emptySound.Play();
            }
        }

        // Run the close-cylinder animation, and land on a random bullet position if enabled in module settings.
        private IEnumerator SpinToRandom()
        {
            Animate(module.closingAnim);
            do yield return null;
            while (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.closingAnim));
            if (animation.speed != prevAnimSpeed)
            {
                animation.speed = prevAnimSpeed;
            }

            if (module.spinToRandomBullet)
            {
                bulletIndex = Random.Range(0, itemQuiver.holder.definition.slots.Count);
                Animate(module.idleAnimPrefix + bulletIndex);
                //Debug.Log("[F-L42] Spinning to random bullet position: " + bulletIndex);
            }
        }

        // Core Framework Functions //
        public void PreFireEffects()
        {
            if (muzzleFlash != null) muzzleFlash.Play();
            if (shotSound != null) shotSound.Play();
            //if (smoke != null) smoke.Play();
        }

        // Spawn an item and launch it as a projectile. Based on a function from TOR by `Kingo64`
        public void SpawnProjectile(string projectileID)
        {
            var projectileData = Catalog.current.GetData<ItemData>(projectileID, true);
            if (projectileData == null) return;
            else
            {
                projectile = projectileData.Spawn(true);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                projectile.Throw(1.0f);
                projectile.transform.position = muzzlePoint.position;
                projectile.transform.rotation = Quaternion.Euler(muzzlePoint.rotation.eulerAngles);
                projectileBody = projectile.GetComponent<Rigidbody>();
                projectileBody.velocity = item.GetComponent<Rigidbody>().velocity;
                projectileBody.AddForce(projectileBody.transform.forward * 1000.0f * module.bulletForce);
            }
        }

        // Add positional recoil to the gun. Based on a function from TOR by `Kingo64`
        public void ApplyRecoil()
        {
            if (gunGripHeldRight) PlayerControl.handRight.HapticShort(module.hapticForce);
            if (gunGripHeldLeft) PlayerControl.handLeft.HapticShort(module.hapticForce);
            
            if (module.recoilForces != null)
            {
                Rb.AddRelativeForce(new Vector3(
                    Random.Range(module.recoilForces[0], module.recoilForces[1]) * module.recoilMult,
                    Random.Range(module.recoilForces[2], module.recoilForces[3]) * module.recoilMult,
                    Random.Range(module.recoilForces[4], module.recoilForces[5]) * module.recoilMult));
            }
        }

        // Interaction Functions //
        public void OnGripGrabbed(Interactor interactor, EventTime arg2)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            else if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
        }

        public void OnGripUnGrabbed(Interactor interactor, EventTime arg2)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            else if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
        }

    }
}
