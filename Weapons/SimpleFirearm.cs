using System;
using System.Collections.Generic;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;
using System.Collections;

namespace ModularFirearms.Weapons
{
    public class SimpleFirearm : MonoBehaviour
    {
        //  ThunderRoad references
        protected Item item;
        protected Shared.FirearmModule module;
        private Handle gunGrip;
        //  Unity references
        private Animator Animations;
        private Transform muzzlePoint;
        private Transform npcRayCastPoint;
        private ParticleSystem MuzzleFlash;
        private ParticleSystem earlyMuzzleFlash;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private AudioSource reloadSound;
        private AudioSource earlyFireSound;
        //  Weapon logic references
        private FireMode fireModeSelection;
        private List<int> allowedFireModes;
        private int remaingingAmmo;
        private bool infAmmo = false;
        private bool isEmpty = false;
        private bool triggerPressed;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;
        public bool isFiring;
        public bool currentlySpawningProjectile;
        bool useRaycast;
        float rayCastMaxDist;
        //  NPC control logic
        Creature thisNPC;
        BrainData thisNPCBrain;
        BrainModuleBow BrainBow;
        BrainModuleMelee BrainMelee;
        BrainModuleDefense BrainParry;
        float npcShootDelay;
        public void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.FirearmModule>();
            // Prioritize local settings, then fetch global settings //
            useRaycast = module.useHitscan ? true : Shared.FrameworkSettings.local.useHitscan;
            rayCastMaxDist = module.useHitscan ? module.hitscanMaxDistance : Shared.FrameworkSettings.local.hitscanMaxDistance;
            if (rayCastMaxDist <= 0f) rayCastMaxDist = Mathf.Infinity;
            try
            {
                if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
                else muzzlePoint = item.transform;
            }
            catch
            {
                Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"muzzlePositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzlePositionRef));
                muzzlePoint = item.transform;
            }
            //  Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"fireSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.fireSoundRef));
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"emptySoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.emptySoundRef));
            if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"reloadSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.reloadSoundRef));
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"swtichSoundRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.swtichSoundRef));
            if (!string.IsNullOrEmpty(module.npcRaycastPositionRef)) npcRayCastPoint = item.GetCustomReference(module.npcRaycastPositionRef);
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"npcRaycastPositionRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.npcRaycastPositionRef));
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"muzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.muzzleFlashRef));
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"animatorRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.animatorRef));
            if (!string.IsNullOrEmpty(module.earlyFireSoundRef)) earlyFireSound = item.GetCustomReference(module.earlyFireSoundRef).GetComponent<AudioSource>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryFireSound\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyFireSoundRef));
            if (!string.IsNullOrEmpty(module.earlyMuzzleFlashRef)) earlyMuzzleFlash = item.GetCustomReference(module.earlyMuzzleFlashRef).GetComponent<ParticleSystem>();
            else Debug.LogError(string.Format("[SimpleFirearmsFramework] Exception: '\"secondaryMuzzleFlashRef\": \"{0}\"' was set in JSON, but \"{0}\" is not present on the Unity Prefab.", module.earlyMuzzleFlashRef));
            if (npcRayCastPoint == null) { npcRayCastPoint = muzzlePoint; }
            // Setup ammo tracking 
            if (module.ammoCapacity > 0) remaingingAmmo = (int)module.ammoCapacity;
            else infAmmo = true;
            // Override SFX volume from JSON
            if ((module.soundVolume > 0.0f) && (module.soundVolume <= 1.0f))
            {
                if (fireSound != null) fireSound.volume = module.soundVolume;
            }
            if (module.loopedFireSound) fireSound.loop = true;
            // Get firemode based on numeric index of the enum
            fireModeSelection = (FireMode)Enum.Parse(typeof(FireMode), module.fireMode);
            if (module.allowedFireModes != null) allowedFireModes = new List<int>(module.allowedFireModes);
            // Handle interaction events
            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip == null)
            {
                // If not defined, get the first handle named "Handle", and if still not found try to get the first object with a Handle component
                gunGrip = item.transform.Find("Handle").GetComponent<Handle>();
                if (gunGrip == null) gunGrip = item.GetComponentInChildren<Handle>();
            }
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (handle.Equals(gunGrip))
            {
                if (action == Interactable.Action.UseStart)
                {
                    if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

                    triggerPressed = true;
                    if (module.isFlintlock)
                    {
                        StartCoroutine(FlintlockLinkedFire());
                    }
                    else if (!isFiring) StartCoroutine(GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag, ProjectileIsSpawning));
                }
                if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
                {
                    // Stop Firing.
                    triggerPressed = false;
                }
                if (action == Interactable.Action.AlternateUseStart)
                {
                    if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

                    if (module.allowCycleFireMode && !isEmpty)
                    {
                        if (emptySound != null) emptySound.Play();
                        fireModeSelection = FrameworkCore.CycleFireMode(fireModeSelection, allowedFireModes);
                        //SetFireSelectionAnimator(Animations, fireModeSelection);
                    }
                    else if (isEmpty)
                    {
                        // Reload the weapon
                        ReloadWeapon();
                    }
                }
            }
        }

        public void OnMainGripGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = true;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = true;

            if (!gunGripHeldLeft && !gunGripHeldRight)
            {
                if (isEmpty)
                {
                    ReloadWeapon();
                }
                thisNPC = interactor.ragdoll.creature;
                thisNPCBrain = thisNPC.brain.instance;
                BrainBow = thisNPCBrain.GetModule<BrainModuleBow>();
                BrainMelee = thisNPCBrain.GetModule<BrainModuleMelee>();
                BrainParry = thisNPCBrain.GetModule<BrainModuleDefense>();
                thisNPC.brain.currentTarget = Player.local.creature;
                thisNPC.brain.isDefending = true;

                BrainMelee.meleeEnabled = module.npcMeleeEnableOverride;
            }
        }

        public void OnMainGripUnGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;

            if (thisNPC != null)
            {
                thisNPC = null;
                thisNPCBrain = null;
                BrainBow = null;
                BrainMelee = null;
                BrainParry = null;
            }
        }

        public void LateUpdate()
        {
            if (module.loopedFireSound)
            {
                bool t = TriggerIsPressed();
                if (t && !fireSound.isPlaying)
                {
                    fireSound.Play();
                }
                else if ((!t && fireSound.isPlaying) || (isEmpty)) fireSound.Stop();
            }
            if (BrainParry != null)
            {
                if (thisNPC.brain.currentTarget != null)
                {
                    //BrainParry.StartParry(thisNPC.brain.currentTarget);
                    BrainParry.StartDefense();
                    if (!module.npcMeleeEnableOverride)
                    {
                        BrainMelee.meleeEnabled = Vector3.Distance(item.rb.position, thisNPC.brain.currentTarget.transform.position) <= module.npcMeleeEnableDistance;
                    }
                }
            }
            if (npcShootDelay > 0) npcShootDelay -= Time.deltaTime;
            if (npcShootDelay <= 0) { NPCFire(); }
        }

        private void ReloadWeapon()
        {
            if (module.waitForReloadAnim && IsAnimationPlaying(Animations, module.reloadAnim)) return;

            if (reloadSound != null) reloadSound.Play();

            Animate(Animations, module.reloadAnim);

            remaingingAmmo = module.ammoCapacity;
            isEmpty = false;
        }

        public void SetFiringFlag(bool status)
        {
            isFiring = status;
        }

        void NPCFire()
        {
            if (thisNPC != null && thisNPCBrain != null && thisNPC.brain.currentTarget != null)
            {
                var npcAimAngle = NpcAimingAngle(BrainBow, npcRayCastPoint.TransformDirection(Vector3.forward), module.npcDistanceToFire);
                if (Physics.Raycast(npcRayCastPoint.position, npcAimAngle, out RaycastHit hit, module.npcDetectionRadius))
                {
                    Creature target = null;
                    if (hit.collider.transform.root.name.Contains("Player") || hit.collider.transform.root.name.Contains("Pool_Human"))
                        target = hit.collider.transform.root.GetComponentInChildren<Creature>();
                    if (target != null && thisNPC != target
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Passive
                        && target.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored
                        && (thisNPC.faction.attackBehaviour == GameData.Faction.AttackBehaviour.Agressive || thisNPC.factionId != target.factionId))
                    {
                        Fire(true);
                        DamageCreatureCustom(target, module.npcDamageToPlayer, hit.point);
                        npcShootDelay = UnityEngine.Random.Range(BrainBow.minMaxTimeBetweenAttack.x, BrainBow.minMaxTimeBetweenAttack.y);
                    }
                }
            }
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public bool ProjectileIsSpawning() { return currentlySpawningProjectile; }

        public void PreFireEffects()
        {
            if (MuzzleFlash != null) MuzzleFlash.Play();
            if (remaingingAmmo == 1)
            {
                // Last Shot
                Animate(Animations, module.emptyAnim);
                isEmpty = true;
            }
            else
            {
                Animate(Animations, module.fireAnim);
            }
            if (!module.loopedFireSound && fireSound != null) fireSound.Play();
        }

        private void Fire(bool firedByNPC = false, bool playEffects = true)
        {
            if (playEffects) PreFireEffects();
            if (firedByNPC) return;
            if (!useRaycast || !ShootRaycastDamage(muzzlePoint, module.bulletForce, rayCastMaxDist))
            {
                ShootProjectile(this.item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult);
            }
            ApplyRecoil(item.rb, module.recoilForces, module.throwMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
        }

        public bool TrackedFlintlockFire()
        {
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                if (MuzzleFlash != null) MuzzleFlash.Play();
                if (fireSound != null) fireSound.Play();
                if (remaingingAmmo == 1)
                {
                    isEmpty = true;
                }
                Fire(false, false);
                remaingingAmmo--;
                return true;
            }
            return false;
        }

        public IEnumerator FlintlockLinkedFire()
        {
            // Fire Success
            if (!isEmpty && (infAmmo || remaingingAmmo > 0))
            {
                Animate(Animations, module.fireAnim);
                if (module.waitForFireAnim)
                {
                    do yield return null;
                    while (IsAnimationPlaying(Animations, module.fireAnim));
                }

                if (remaingingAmmo == 1)
                {
                    isEmpty = true;
                }
                if (earlyMuzzleFlash != null) earlyMuzzleFlash.Play();
                if (earlyFireSound != null) earlyFireSound.Play();
                yield return new WaitForSeconds(module.flintlockDelay);
                Fire();
                remaingingAmmo--;
            }
            // Fire Failure
            else
            {
                if (emptySound != null) emptySound.Play();
                yield return null;
            }

            yield return null;

        }

        private IEnumerator FlintlockFireDelay(bool waitForFireAnim, float secondaryDelay)
        {

            yield return new WaitForSeconds(secondaryDelay);
        }

        public bool TrackedFire()
        {
            // Returns 'true' if Fire was successful.
            if (isEmpty) return false;
            if (infAmmo || remaingingAmmo > 0)
            {
                Fire();
                remaingingAmmo--;
                return true;
            }
            return false;
        }

    }
}
