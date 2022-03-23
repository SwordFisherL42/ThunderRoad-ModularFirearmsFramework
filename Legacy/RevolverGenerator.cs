﻿
// Modular revolver base class, handling all extended interactions with other classes.

/* Description: Modular revolver base class, handling all extended interactions with other classes.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 08/20/2020
 * 
 */

//namespace ModularFirearms.Weapons
//{
//    public class RevolverGenerator : MonoBehaviour
//    {
//        // ThunderRoad References
//        protected Item item;
//        protected ItemModuleFirearmRevolver module;
//        protected ItemQuiver itemQuiver;
//        protected ItemModuleQuiver moduleQuiver;
//        protected Holder holder;
//        protected Handle gunGrip;
//        // Unity References
//        protected Transform muzzlePoint;
//        protected Animator animations;
//        protected ParticleSystem muzzleFlash;
//        protected AudioSource shotSound;
//        protected AudioSource emptySound;
//        protected AudioSource reloadSound;
//        protected AudioSource spinSound;
//        protected AudioSource latchSound;
//        // Logic Vars
//        private string insertedSpellID;
//        protected bool gunGripHeldLeft;
//        protected bool gunGripHeldRight;
//        protected bool isOpen;
//        protected bool bulletVisible;
//        protected bool rightHapticFlag = false;
//        protected bool leftHapticFlag = false;
//        protected int bulletIndex;
//        protected float prevAnimSpeed;
//        protected float lastVelocity = 0f;

//        protected void Awake()
//        {
//            // Basic item references
//            item = this.GetComponent<Item>();
//            module = item.data.GetModule<ItemModuleFirearmRevolver>();
//            if (module.muzzlePositionRef != null) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
//            if (module.flashRef != null) muzzleFlash = item.GetCustomReference(module.flashRef).GetComponent<ParticleSystem>();
//            if (module.soundNames[0] != null) shotSound = item.GetCustomReference(module.soundNames[0]).GetComponent<AudioSource>();
//            if (module.soundNames[1] != null) emptySound = item.GetCustomReference(module.soundNames[1]).GetComponent<AudioSource>();
//            if (module.soundNames[2] != null) reloadSound = item.GetCustomReference(module.soundNames[2]).GetComponent<AudioSource>();
//            if (module.soundNames[3] != null) spinSound = item.GetCustomReference(module.soundNames[3]).GetComponent<AudioSource>();
//            if (module.soundNames[4] != null) latchSound = item.GetCustomReference(module.soundNames[4]).GetComponent<AudioSource>();
//            if (module.gunGripID != null) gunGrip = item.GetCustomReference(module.gunGripID).GetComponent<Handle>();
//            // Setup item events
//            item.OnHeldActionEvent += OnHeldAction;
//            if (gunGrip != null)
//            {
//                gunGrip.Grabbed += OnGripGrabbed;
//                gunGrip.UnGrabbed += OnGripUnGrabbed;
//            }
//            // Animations
//            if (module.animatorRef != null)
//            {
//                animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>();
//                if (module.animationSpeed > 0) animations.speed = animations.speed * module.animationSpeed;
//                prevAnimSpeed = animations.speed;
//            }
//            // Quiver (Chamber) definitions
//            itemQuiver = this.GetComponent<ItemQuiver>();
//            moduleQuiver = item.data.GetModule<ItemModuleQuiver>();
//            holder = itemQuiver.GetComponentInChildren<Holder>();
//            holder.Snapped += new Holder.HolderDelegate(this.OnProjectileAdded);
//            itemQuiver.holder.data.disableTouch = true;
//            isOpen = false;

//            reloadSound.clip = holder.audioContainer.sounds[0];

//        }

//        private void FixedUpdate()
//        {
//            // Check for flip-to-close gesture on FixedUpdate
//            if (!module.gestureEnabled) return;

//            if ((Mathf.Abs(item.rb.velocity.x - lastVelocity) > module.minGestureVelocity) && (isOpen))
//            {
//                prevAnimSpeed = animations.speed;
//                animations.speed = animations.speed * 2;
//                CloseChamber();
//            }
//            lastVelocity = item.rb.velocity.x;
//        }

//        protected void OnProjectileAdded(Item interactiveObject)
//        {
//            insertedSpellID = null;
//            try
//            {
//                Items.InteractiveAmmo insertedAmmo = interactiveObject.GetComponent<Items.InteractiveAmmo>();
//                FirearmFunctions.AmmoType insertedAmmoType = insertedAmmo.GetAmmoType();

//                itemQuiver.holder.UnSnap(interactiveObject);

//                // ItemAmmoLoader addedLoader = interactiveObject.GetComponent<ItemAmmoLoader>();
//                if ((insertedAmmoType == FirearmFunctions.AmmoType.AmmoLoader)||(insertedAmmoType == FirearmFunctions.AmmoType.Revolver))
//                {
//                    int loaderCount = insertedAmmo.GetAmmoCount();
//                    reloadSound.Play();
//                    if (loaderCount >= itemQuiver.holder.slots.Count)
//                    {
//                        itemQuiver.SpawnAllProjectiles();
//                    }
//                    else
//                    {
//                        for (int i = 0; i < loaderCount; i++)
//                        {
//                            itemQuiver.SpawnProjectile();
//                        }
//                    }
//                    insertedSpellID = FirearmFunctions.GetItemSpellChargeID(interactiveObject);
//                    if (!string.IsNullOrEmpty(insertedSpellID))
//                    {
//                        SpellCastCharge transferedSpell = Catalog.GetData<SpellCastCharge>(insertedSpellID, true).Clone();
//                        foreach (Imbue itemImbue in item.imbues)
//                        {
//                            StartCoroutine(FirearmFunctions.TransferDeltaEnergy(itemImbue, transferedSpell));
//                        }
//                    }
//                }

//                interactiveObject.Despawn();
//                return;
//                // ItemAmmo insertedAmmo = interactiveObject.GetComponent<ItemAmmo>();
//                //else if (insertedAmmo.GetAmmoType() == FirearmFunctions.AmmoType.Revolver)
//                //{
//                //    itemQuiver.holder.UnSnap(interactiveObject);
//                //    interactiveObject.Despawn();
//                //    return;
//                //}
//                //else
//                //{
//                //    itemQuiver.holder.UnSnap(interactiveObject);
//                //    return;
//                //}

//                //insertedSpellID = FirearmFunctions.GetItemSpellChargeID(interactiveObject);
//                //if (!string.IsNullOrEmpty(insertedSpellID))
//                //{
//                //    SpellCastCharge transferedSpell = Catalog.GetData<SpellCastCharge>(insertedSpellID, true).Clone();
//                //    foreach (Imbue itemImbue in item.imbues)
//                //    {
//                //        StartCoroutine(FirearmFunctions.TransferDeltaEnergy(itemImbue, transferedSpell));
//                //    }
//                //}
//            }
//            catch { Debug.Log("[ModularFirearmsFramework][ERROR] Exception in Adding Projectile."); }
//        }

//        protected void TrackedFire()
//        {
//            // Method for Player Firing the weapon. Tracks bullet position/state, and performs the associated actions for each state
//            Items.InteractiveAmmo firedAmmo = null;
//            // If you are firing on a position that has no bullet or shell, move to next position but still animate the chamber/play the `empty sound`.

//            if (bulletIndex <= itemQuiver.holder.items.Count - 1) firedAmmo = itemQuiver.holder.items[bulletIndex].GetComponent<Items.InteractiveAmmo>();
//            else
//            {
//                StartCoroutine(AnimateAndFire(bulletIndex, true));
//                bulletIndex++;
//                if (bulletIndex >= itemQuiver.holder.slots.Count) bulletIndex = 0;
//                return;
//            }
//            // If you are firing on a position that has an `ItemAmmo` item, check if the bullet is loaded or spent.
//            if (firedAmmo != null)
//            {
//                if (firedAmmo.isLoaded)
//                {
//                    StartCoroutine(AnimateAndFire(bulletIndex, false));
//                    firedAmmo.Consume();
//                    bulletIndex++;
//                }
//                else
//                {
//                    StartCoroutine(AnimateAndFire(bulletIndex, true));
//                    bulletIndex++;
//                }
//                if (bulletIndex >= itemQuiver.holder.slots.Count) bulletIndex = 0;
//            }
//            else Debug.Log("[ModularFirearmsFramework][ERROR] Logical Exception, ammo was somehow NULL...");
//        }

//        public bool Fire()
//        {
//            PreFireEffects();
//            FirearmFunctions.ShootProjectile(item, module.projectileID, muzzlePoint, FirearmFunctions.GetItemSpellChargeID(item), module.bulletForce, module.throwMult);
//            FirearmFunctions.ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
//            return true;
//        }

//        public void PreFireEffects()
//        {
//            if (muzzleFlash != null) muzzleFlash.Play();
//            if (shotSound != null) shotSound.Play();
//        }

//        public void ToggleChamber()
//        {
//            if (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.fireAnimPrefix)) return;
//            if ((!isOpen) && (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.idleAnimPrefix))) OpenChamber();
//            else if (isOpen && (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name == module.idleOpenAnim)) CloseChamber();
//        }

//        public void OpenChamber()
//        {
//            isOpen = true;
//            itemQuiver.holder.data.disableTouch = false;
//            prevAnimSpeed = animations.speed;
//            animations.speed = animations.speed * 2;
//            if (latchSound != null) latchSound.Play();
//            FirearmFunctions.Animate(animations, module.openingAnim);
//            animations.speed = prevAnimSpeed;
//        }

//        public void CloseChamber()
//        {
//            itemQuiver.holder.data.disableTouch = true;
//            isOpen = false;
//        }

//        private IEnumerator AnimateAndFire(int spindex, bool misfire = false)
//        {
//            if (spinSound != null) spinSound.Play();
//            StartCoroutine(SpinToRandom());
//            FirearmFunctions.Animate(animations, module.fireAnimPrefix + spindex);
//            if (!module.lockFiringToAnimation)
//            {
//                yield return new WaitForSeconds(module.firingDelay);
//                if (!misfire) Fire();
//                else emptySound.Play();
//            }
//            do yield return null;
//            while (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.fireAnimPrefix));
//            if (module.lockFiringToAnimation)
//            {
//                if (!misfire) Fire();
//                else emptySound.Play();
//            }
//        }

//        private IEnumerator SpinToRandom()
//        {
//            // Run the close-cylinder animation, and land on a random bullet position if enabled in module settings.
//            FirearmFunctions.Animate(animations, module.closingAnim);
//            do yield return null;
//            while (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.closingAnim));
//            if (animations.speed != prevAnimSpeed)
//            {
//                animations.speed = prevAnimSpeed;
//            }

//            if (module.spinToRandomBullet)
//            {
//                bulletIndex = Random.Range(0, itemQuiver.holder.slots.Count);
//                FirearmFunctions.Animate(animations, module.idleAnimPrefix + bulletIndex);
//            }
//        }

//        protected void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
//        {
//            // Release all shells if chamber is open, otherwise attempt to fire the weapon.
//            if (action == Interactable.Action.UseStart)
//            {
//                if (isOpen && (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name == module.idleOpenAnim)) foreach (Item chamberItem in itemQuiver.holder.items.ToList()) itemQuiver.holder.UnSnap(chamberItem);
//                else if (animations.GetCurrentAnimatorClipInfo(0)[0].clip.name.Contains(module.idleAnimPrefix)) TrackedFire();
//            }
//            // Alt-Use (Spell-Menu button) toggles the chamber open/close state.
//            if (action == Interactable.Action.AlternateUseStart)
//            {
//                ToggleChamber();
//            }
//        }

//        public void OnGripGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
//        {
//            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
//            else if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;
//        }

//        public void OnGripUnGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
//        {
//            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
//            else if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;
//        }

//    }
//}
