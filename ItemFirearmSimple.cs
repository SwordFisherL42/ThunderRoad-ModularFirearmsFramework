using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

/* Description: An Item plugin for `ThunderRoad` which provides the basic functionality needed
 * to setup a simple ballistic weapon.
 * 
 * author: SwordFisherL42 ("Fisher")
 * date: 08/22/2020
 * 
 */

namespace ModularFirearms
{
    public class ItemFirearmSimple : MonoBehaviour
    {
        //ThunderRoad references
        protected Item item;
        protected ItemModuleFirearmSimple module;
        private Handle gunGrip;
        //Unity references
        private Animator Animations;
        private Transform muzzlePoint;
        private Transform npcRayCastPoint;
        private ParticleSystem MuzzleFlash;
        private AudioSource fireSound;
        private AudioSource emptySound;
        private AudioSource switchSound;
        private AudioSource reloadSound;
        //Weapon logic references
        private FireMode fireModeSelection;
        private int remaingingAmmo;
        private bool infAmmo = false;
        private bool isEmpty = false;
        private bool triggerPressed;
        private bool gunGripHeldLeft;
        private bool gunGripHeldRight;
        public bool isFiring;
        //NPC control logic
        Creature thisNPC;
        BrainHuman thisNPCBrain;
        float npcShootDelay;
        bool npcPrevMeleeEnabled;
        float npcPrevMeleeDistMult;
        float npcPrevParryDetectionRadius;
        float npcPrevParryMaxDist;

        public void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleFirearmSimple>();

            if (!string.IsNullOrEmpty(module.muzzlePositionRef)) muzzlePoint = item.GetCustomReference(module.muzzlePositionRef);
            else muzzlePoint = item.transform;

            //Fetch Animator, ParticleSystem, and AudioSources from Custom References (see "How-To Guide" for more info on custom references)
            if (!string.IsNullOrEmpty(module.fireSoundRef)) fireSound = item.GetCustomReference(module.fireSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.emptySoundRef)) emptySound = item.GetCustomReference(module.emptySoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.swtichSoundRef)) switchSound = item.GetCustomReference(module.swtichSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.reloadSoundRef)) reloadSound = item.GetCustomReference(module.reloadSoundRef).GetComponent<AudioSource>();
            if (!string.IsNullOrEmpty(module.npcRaycastPositionRef)) npcRayCastPoint = item.GetCustomReference(module.npcRaycastPositionRef);
            if (!string.IsNullOrEmpty(module.muzzleFlashRef)) MuzzleFlash = item.GetCustomReference(module.muzzleFlashRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.animatorRef)) Animations = item.GetCustomReference(module.animatorRef).GetComponent<Animator>();

            if (npcRayCastPoint == null) { npcRayCastPoint = muzzlePoint; }

            //Setup ammo tracking 
            if (module.ammoCapacity > 0)
            {
                remaingingAmmo = (int)module.ammoCapacity;
            }
            else
            {
                infAmmo = true;
            }

            //Override SFX volume from JSON
            if ((module.soundVolume > 0.0f) && (module.soundVolume <= 1.0f))
            {
                if (fireSound != null)
                {
                    fireSound.volume = module.soundVolume;
                }
            }

            //Get firemode based on numeric index of the enum
            fireModeSelection = (FireMode)fireModeEnums.GetValue(module.fireMode);

            //Handle interaction events
            item.OnHeldActionEvent += OnHeldAction;
            if (!string.IsNullOrEmpty(module.mainGripID)) gunGrip = item.GetCustomReference(module.mainGripID).GetComponent<Handle>();
            if (gunGrip != null)
            {
                gunGrip.Grabbed += OnMainGripGrabbed;
                gunGrip.UnGrabbed += OnMainGripUnGrabbed;
            }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.UseStart)
            {
                triggerPressed = true;
                if (!isFiring) StartCoroutine(GeneralFire(TrackedFire, TriggerIsPressed, fireModeSelection, module.fireRate, module.burstNumber, emptySound, SetFiringFlag));
            }
            if (action == Interactable.Action.UseStop || action == Interactable.Action.Ungrab)
            {
                //Stop Firing.
                triggerPressed = false;
            }
            if (action == Interactable.Action.AlternateUseStart)
            {
                if (module.allowCycleFireMode && !isEmpty)
                {
                    if (emptySound != null) emptySound.Play();
                    fireModeSelection = CycleFireMode(fireModeSelection);
                }
                else
                {
                    //Reload the weapon
                    ReloadWeapon();
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
                //Debug.Log("[AI SHOOT] Gun held by NPC");
                thisNPC = interactor.ragdoll.creature;
                thisNPCBrain = (BrainHuman) thisNPC.brain.instance;
                npcPrevMeleeEnabled = thisNPCBrain.meleeEnabled;
                if (npcPrevMeleeEnabled)
                {
                    npcPrevMeleeDistMult = thisNPCBrain.meleeMax;
                    npcPrevParryDetectionRadius = thisNPCBrain.parryDetectionRadius;
                    npcPrevParryMaxDist = thisNPCBrain.parryMaxDistance;
                    thisNPCBrain.meleeEnabled = module.npcMeleeEnableFlag;
                    if (!module.npcMeleeEnableFlag)
                    {
                        thisNPCBrain.meleeDistMult = thisNPCBrain.bowDist * module.npcDistanceToFire;
                        thisNPCBrain.parryDetectionRadius = thisNPCBrain.bowDist * module.npcDistanceToFire;
                        thisNPCBrain.parryMaxDistance = thisNPCBrain.bowDist * module.npcDistanceToFire;
                    }
                }

            }

        }

        public void OnMainGripUnGrabbed(RagdollHand interactor, Handle handle, EventTime eventTime)
        {
            if (interactor.playerHand == Player.local.handRight) gunGripHeldRight = false;
            if (interactor.playerHand == Player.local.handLeft) gunGripHeldLeft = false;

            if (thisNPC != null)
            {
                if (npcPrevMeleeEnabled)
                {
                    thisNPCBrain.meleeEnabled = npcPrevMeleeEnabled;
                    thisNPCBrain.meleeDistMult = npcPrevMeleeDistMult;
                    thisNPCBrain.parryMaxDistance = npcPrevParryMaxDist;
                }

                thisNPC = null;
            }

        }

        public void LateUpdate()
        {
            if (npcShootDelay > 0) npcShootDelay -= Time.deltaTime;
            if (npcShootDelay <= 0) { NPCshoot(); }
        }

        private void ReloadWeapon()
        {
            if (reloadSound != null) reloadSound.Play();
            Animate(Animations, module.reloadAnim);
            remaingingAmmo = module.ammoCapacity;
            isEmpty = false;
        }

        public void SetFiringFlag(bool status)
        {
            isFiring = status;
        }

        private void NPCshoot()
        {
            if (thisNPC != null && thisNPCBrain != null && thisNPCBrain.targetCreature != null)
            {
                if (!module.npcMeleeEnableFlag)
                {
                    thisNPCBrain.meleeEnabled = Vector3.Distance(item.rb.position, thisNPCBrain.targetCreature.transform.position) <= (gunGrip.reach + 3f);
                }
                var npcAimAngle = NpcAimingAngle(thisNPCBrain, npcRayCastPoint.TransformDirection(Vector3.forward), module.npcDistanceToFire);
                if (Physics.Raycast(npcRayCastPoint.position, npcAimAngle, out RaycastHit hit, thisNPCBrain.detectionRadius))
                {
                    Creature target = null;
                    target = hit.collider.transform.root.GetComponent<Creature>();
                    if (target != null && thisNPC != target
                        && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored && thisNPC.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Passive
                        && target.faction.attackBehaviour != GameData.Faction.AttackBehaviour.Ignored && (thisNPC.faction.attackBehaviour == GameData.Faction.AttackBehaviour.Agressive || thisNPC.factionId != target.factionId))
                    {
                        Fire();
                        npcShootDelay = Random.Range(thisNPCBrain.bowAimMinMaxDelay.x, thisNPCBrain.bowAimMinMaxDelay.y) * ((thisNPCBrain.bowDist / module.npcDistanceToFire + hit.distance / module.npcDistanceToFire) / thisNPCBrain.bowDist);
                    }
                }
            }
        }

        public bool TriggerIsPressed() { return triggerPressed; }

        public void PreFireEffects()
        {
            if (MuzzleFlash != null) MuzzleFlash.Play();
            // Last Shot
            if (remaingingAmmo == 1)
            {
                Animate(Animations, module.emptyAnim);
                isEmpty = true;
            }
            else
            {
                Animate(Animations, module.fireAnim);
            }
            if (fireSound != null) fireSound.Play();
        }

        private void Fire()
        {
            PreFireEffects();
            ShootProjectile(item, module.projectileID, muzzlePoint, GetItemSpellChargeID(item), module.bulletForce, module.throwMult);
            ApplyRecoil(item.rb, module.recoilForces, module.recoilMult, gunGripHeldLeft, gunGripHeldRight, module.hapticForce);
        }

        public bool TrackedFire()
        {
            //Returns 'true' if Fire was successful.
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
