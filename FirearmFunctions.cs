using UnityEngine;
using ThunderRoad;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ModularFirearms
{

    /// <summary>
    /// Represents a function that:
    /// 1) Attempts to shoot a projectile and play particle/haptic effects
    /// 2) Returns a bool representing if that attempt is successful or not (i.e. no ammo, etc)
    /// </summary>
    /// <returns></returns>
    public delegate bool TrackFiredDelegate();

    /// <summary>
    /// Represents a function that determines if the trigger is currently pressed.
    /// </summary>
    /// <returns></returns>
    public delegate bool TriggerPressedDelegate();

    /// <summary>
    /// Represents a function that determines if weapon is firing
    /// </summary>
    /// <returns></returns>
    public delegate void IsFiringDelegate(bool status);

    /// <summary>
    /// Core Framework Functions, meant to be shared across multiple classes
    /// </summary>
    public class FirearmFunctions
    {
        /// <summary>
        /// Provide static points for 2D cartesian blend tree, to be used for firemode selection states.
        /// Indicies match the corresponding FireMode enum, i.e. Misfire, Single, Burst, Auto
        /// </summary>
        public static float[,] blendTreePositions = new float[4, 2] { { 0.0f, 0.0f }, { 0.0f, 1.0f }, { 1.0f, 0.0f }, { 1.0f, 1.0f } };

        public static string projectileColliderReference = "BodyCollider";

        /// <summary>
        /// Defines which behaviour should be produced at runtime
        /// </summary>
        public enum FireMode
        {
            /// <summary>
            /// Used for when the weapon is in safe-mode or is unable to fire for other reasons
            /// </summary>
            Misfire = 0,
            /// <summary>
            /// Used for a single-shot, semi-auto weapon behaviour
            /// </summary>
            Single = 1,
            /// <summary>
            /// Used for x-Round burst weapon behaviour
            /// </summary>
            Burst = 2,
            /// <summary>
            /// Used for full automatic weapon behaviour
            /// </summary>
            Auto = 3
        }

        /// <summary>
        /// A static array useful for accessing FireMode enums by index
        /// </summary>
        public static Array fireModeEnums = Enum.GetValues(typeof(FireMode));

        /// <summary>
        /// A static array useful for accessing ForceMode enums by index
        /// </summary>
        public static Array forceModeEnums = Enum.GetValues(typeof(ForceMode));

        /// <summary>
        /// Take a given FireMode and return an increment/loop to the next enum value
        /// </summary>
        /// <param name="currentSelection"></param>
        /// <returns></returns>
        public static FireMode CycleFireMode(FireMode currentSelection, List<int> allowedFireModes = null)
        {
            int selectionIndex = (int) currentSelection;
            selectionIndex++;
            if (allowedFireModes != null)
            {
                foreach (var _ in Enumerable.Range(0, fireModeEnums.Length))
                {
                    if (allowedFireModes.Contains(selectionIndex)) return (FireMode)fireModeEnums.GetValue(selectionIndex);
                    selectionIndex++;
                    if (selectionIndex >= fireModeEnums.Length) selectionIndex = 0;
                }
                return currentSelection;
            }
            else
            {
                if (selectionIndex < fireModeEnums.Length) return (FireMode)fireModeEnums.GetValue(selectionIndex);
                else return (FireMode)fireModeEnums.GetValue(0);
            }

        }

        /// <summary>
        /// Wrapper method for taking an animator and playing an animation if it exists
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="animationName"></param>
        /// <returns></returns>
        public static bool Animate(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return false;
            animator.Play(animationName);
            return true;
        }

        /// <summary>
        /// Apply positional recoil to a rigid body. Optionally, apply haptic force to the player controllers.
        /// </summary>
        /// <param name="itemRB"></param>
        /// <param name="recoilForces"></param>
        /// <param name="recoilMult"></param>
        /// <param name="leftHandHaptic"></param>
        /// <param name="rightHandHaptic"></param>
        /// <param name="hapticForce"></param>
        public static void ApplyRecoil(Rigidbody itemRB, float[] recoilForces, float recoilMult=1.0f, bool leftHandHaptic=false, bool rightHandHaptic = false, float hapticForce=1.0f)
        {
            if (rightHandHaptic) PlayerControl.handRight.HapticShort(hapticForce);
            if (leftHandHaptic) PlayerControl.handLeft.HapticShort(hapticForce);

            if (recoilForces == null) return;
            itemRB.AddRelativeForce(new Vector3(
                UnityEngine.Random.Range(recoilForces[0], recoilForces[1]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[2], recoilForces[3]) * recoilMult,
                UnityEngine.Random.Range(recoilForces[4], recoilForces[5]) * recoilMult));
        }

        /// <summary>
        /// Use the scene collections of Items and Creatures to apply a normalized force to all rigid bodies within range.
        /// Additionally, apply logic for killing Creatures in range.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="force"></param>
        /// <param name="blastRadius"></param>
        /// <param name="liftMult"></param>
        /// <param name="forceMode"></param>
        public static void HitscanExplosion(Vector3 origin, float force, float blastRadius, float liftMult, ForceMode forceMode = ForceMode.Impulse)
        {

            foreach (Item item in Item.list)
            {
                if (Math.Abs(Vector3.Distance(item.transform.position, origin)) <= blastRadius)
                {
                    //Debug.Log("[F-L42-HitscanExplosion] Hit Item: " + item.name);
                    item.rb.AddExplosionForce(force * item.rb.mass, origin, blastRadius, liftMult, forceMode);
                    item.rb.AddForce(Vector3.up * liftMult * item.rb.mass, forceMode);
                }
            }

            foreach (Creature creature in Creature.list)
            {
                if (creature == Creature.player) continue;
                if (Math.Abs(Vector3.Distance(creature.transform.position, origin)) <= blastRadius)
                {
                    //Debug.Log("[F-L42-HitscanExplosion] Hit Creature: " + creature.name);
                    if (!creature.health.isKilled)
                    {
                        creature.ragdoll.SetState(Creature.State.Dead);
                        creature.health.Kill();
                    }
                    creature.locomotion.rb.AddExplosionForce(force * creature.locomotion.rb.mass, origin, blastRadius, liftMult, forceMode);
                    creature.locomotion.rb.AddForce(Vector3.up * liftMult * creature.locomotion.rb.mass, forceMode);
                    foreach (RagdollPart part in creature.ragdoll.parts)
                    {
                        part.rb.AddExplosionForce(force * part.rb.mass, origin, blastRadius, liftMult, forceMode);
                        part.rb.AddForce(Vector3.up * liftMult *part.rb.mass, forceMode);
                    }
                }
            }

        }

        /// <summary>
        /// Sets floats on an Animator, assuming these floats correspong to 2D cartesian coordinates on a blend tree attached to that animator.
        /// See reference for 'blendTreePositions' for more details. 
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="selection"></param>
        /// <param name="paramFloat1"></param>
        /// <param name="paramFloat2"></param>
        public static void SetFireSelectionAnimator(Animator animator, FireMode selection, string paramFloat1="x", string paramFloat2 = "y")
        {
            if (animator == null) return;
            try
            {
                animator.SetFloat(paramFloat1, blendTreePositions[(int)selection, 0]);
                animator.SetFloat(paramFloat2, blendTreePositions[(int)selection, 1]);
            }
            catch { Debug.LogError("[FL42-FirearmFunctions][SetSwitchAnimation] Exception in setting Animator floats 'x' and 'y'"); }
        }

        /// <summary>
        /// Spawn a projectile from the item catalog, optionally imbue it and propel it forward with a given force
        /// </summary>
        /// <param name="shooterItem"></param>
        /// <param name="projectileID"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="imbueSpell"></param>
        /// <param name="forceMult"></param>
        /// <param name="throwMult"></param>
        /// <param name="pooled"></param>
        public static void ShootProjectile(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell=null, float forceMult=1.0f, float throwMult=1.0f, bool pooled=false, Collider IgnoreArg1 = null)
        {
            var projectileData = Catalog.GetData<ItemPhysic>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                Item projectile = projectileData.Spawn(pooled);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                projectile.IgnoreObjectCollision(shooterItem);
                projectile.IgnoreRagdollCollision(Player.local.body.creature.ragdoll);
                if (IgnoreArg1 != null)
                {
                    try { Physics.IgnoreCollision(IgnoreArg1, projectile.definition.GetCustomReference(projectileColliderReference).GetComponent<Collider>()); }
                    catch { }
                }
                if (!String.IsNullOrEmpty(imbueSpell))
                {
                    // Set imbue charge on projectile using ItemProjectileSimple subclass
                    ItemSimpleProjectile projectileController = projectile.gameObject.GetComponent<ItemSimpleProjectile>();
                    if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                }
                // Match the Position, Rotation, & Speed of the spawner item
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                projectile.rb.velocity = shooterItem.rb.velocity;
                projectile.rb.AddForce(projectile.rb.transform.forward * 1000.0f * forceMult);
                projectile.Throw(throwMult, Item.FlyDetection.CheckAngle);
            }
        }

        public static void ShotgunBlast(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell= null, float forceMult = 1.0f, float throwMult = 1.0f, bool pooled = false, Collider IgnoreArg1 = null)
        {
            var projectileData = Catalog.GetData<ItemPhysic>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                Item projectile = projectileData.Spawn(pooled);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                projectile.IgnoreObjectCollision(shooterItem);
                projectile.IgnoreRagdollCollision(Player.local.body.creature.ragdoll);
                if (IgnoreArg1 != null)
                {
                    try { Physics.IgnoreCollision(IgnoreArg1, projectile.definition.GetCustomReference(projectileColliderReference).GetComponent<Collider>()); }
                    catch { }
                }
                if (!String.IsNullOrEmpty(imbueSpell))
                {
                    // Set imbue charge on projectile using ItemProjectileSimple subclass
                    ItemSimpleProjectile projectileController = projectile.gameObject.GetComponent<ItemSimpleProjectile>();
                    if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                }
                // Match the Position, Rotation, & Speed of the spawner item
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                projectile.rb.velocity = shooterItem.rb.velocity;
                projectile.rb.AddForce(projectile.rb.transform.forward * 1000.0f * forceMult);
                projectile.Throw(throwMult, Item.FlyDetection.CheckAngle);
            }
        }

        /// <summary>
        /// Iterate through the Imbues on an Item and return the first charged SpellID found.
        /// </summary>
        /// <param name="interactiveObject">Item class, representing an interactive game object</param>
        /// <returns></returns>
        public static string GetItemSpellChargeID(Item interactiveObject)
        {
            try
            {
                foreach (Imbue itemImbue in interactiveObject.imbues)
                {
                    if (itemImbue.spellCastBase != null)
                    {
                        return itemImbue.spellCastBase.id;
                    }
                }
                return null;
            }

            catch { return null; }
        }

        /// <summary>
        /// Returns the first Imbue component for a given item, if it exists
        /// </summary>
        /// <param name="imbueTarget">Item class, representing an interactive game object</param>
        /// <returns>Imbue class, which can be used to transfer spells to the object</returns>
        public static Imbue GetFirstImbue(Item imbueTarget)
        {
            try
            {
                if (imbueTarget.imbues.Count > 0) return imbueTarget.imbues[0];
            }
            catch { return null; }
            return null;
        }

        /// <summary>
        /// Determines the accuracy of an NPC, based on brain settings
        /// </summary>
        /// <param name="NPCBrain"></param>
        /// <param name="initial"></param>
        /// <param name="npcDistanceToFire"></param>
        /// <returns></returns>
        public static Vector3 NpcAimingAngle(BrainHuman NPCBrain, Vector3 initial, float npcDistanceToFire = 10.0f)
        {
            if (NPCBrain == null) return initial;
            var inaccuracyMult = 0.2f * (NPCBrain.aimSpreadCone / npcDistanceToFire);
            return new Vector3(
                        initial.x + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.y + (UnityEngine.Random.Range(-inaccuracyMult, inaccuracyMult)),
                        initial.z);
        }

        /// <summary>
        /// Transfer energy to a weapon Imbue, over given energy/step deltas and a fixed time delta.
        /// </summary>
        /// <param name="itemImbue">Target Imbue that will accept the SpellCastCharge</param>
        /// <param name="activeSpell">SpellCastCharge that will be transfered to Imbue</param>
        /// <param name="energyDelta">Units of energy transfered each step</param>
        /// <param name="counts">Number of steps</param>
        /// <returns></returns>
        public static IEnumerator TransferDeltaEnergy(Imbue itemImbue, SpellCastCharge activeSpell, float energyDelta = 20.0f, int counts = 5)
        {
            if (activeSpell != null)
            {
                for (int i = 0; i < counts; i++)
                {
                    try { itemImbue.Transfer(activeSpell, energyDelta); }
                    catch { }
                    yield return new WaitForSeconds(0.01f);
                }
            }
            yield return null;
        }

        /// <summary>
        /// Based on FireMode enum, perform the expected behaviours.
        /// Assuming fireRate as Rate-Per-Minute, convert to adequate deylay between shots, given by fD = 1/(fR/60) 
        /// </summary>
        /// <param name="TrackedFire">A function delegated from the weapon to be called for each "shot". This function is expected to return a bool representing if the shot was successful.</param>
        /// <param name="TriggerPressed">A function delegated from the weapon to return a bool representing if the weapon trigger is currently pressed</param>
        /// <param name="fireSelector">FireMode enum, used to determine the behaviour of the method</param>
        /// <param name="fireRate">Determines the delay between calls to `TrackedFire`, given as a Rounds-Per-Minunte value</param>
        /// <param name="burstNumber">The number of  calls  made to `TrackedFire`, if `fireSelector` is set to `FireMode.Burst`</param>
        /// <param name="emptySoundDriver">If `TrackedFire` returns a false, this AudioSource is played</param>
        /// <param name="WeaponIsFiring">A function delegated from the weapon to determine if the coroutine is running</param>
        /// <returns></returns>
        public static IEnumerator GeneralFire(TrackFiredDelegate TrackedFire, TriggerPressedDelegate TriggerPressed, FireMode fireSelector = FireMode.Single, int fireRate = 60, int burstNumber = 3, AudioSource emptySoundDriver = null, IsFiringDelegate WeaponIsFiring = null)
        {
            WeaponIsFiring?.Invoke(true);
            float fireDelay = 60.0f / (float)fireRate;

            if (fireSelector == FireMode.Misfire)
            {
                if (emptySoundDriver != null) emptySoundDriver.Play();
                yield return null;
            }

            else if (fireSelector == FireMode.Single)
            {
                if (!TrackedFire())
                {
                    if (emptySoundDriver != null) emptySoundDriver.Play();
                    yield return null;
                }
                yield return new WaitForSeconds(fireDelay);
            }

            else if (fireSelector == FireMode.Burst)
            {
                for (int i = 0; i < burstNumber; i++)
                {
                    if (!TrackedFire())
                    {
                        if (emptySoundDriver != null) emptySoundDriver.Play();
                        yield return null;
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
                yield return null;
            }

            else if (fireSelector == FireMode.Auto)
            {
                // triggerPressed is handled in OnHeldAction(), so stop firing once the trigger/weapon is released
                while (TriggerPressed())
                {
                    if (!TrackedFire())
                    {
                        if (emptySoundDriver != null) emptySoundDriver.Play();
                        yield return null;
                        break;
                    }
                    yield return new WaitForSeconds(fireDelay);
                }
            }
            WeaponIsFiring?.Invoke(false);
            yield return null;
        }

    }
}
