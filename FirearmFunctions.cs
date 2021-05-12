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
    /// Represents a function that determines if a projectile is currently spawning
    /// </summary>
    /// <returns></returns>
    public delegate bool IsSpawningDelegate();

    /// <summary>
    /// Represents a function that sets the projectile spawning flag
    /// </summary>
    /// <param name="status"></param>
    public delegate void SetSpawningStatusDelegate(bool status);

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

        public static Vector3[] buckshotOffsetPosiitions = new Vector3[5] { Vector3.zero, new Vector3(0.05f, 0.05f, 0.0f), new Vector3(-0.05f, -0.05f, 0.0f), new Vector3(0.05f, -0.05f, 0.0f), new Vector3(0.07f, 0.07f, 0.0f) };

        public static string projectileColliderReference = "BodyCollider";

        public enum WeaponType
        {
            AutoMag = 0,
            SemiAuto = 1,
            Shotgun = 2,
            BoltAction = 3,
            Revolver = 4,
            Sniper = 5,
            HighYield = 6,
            Energy = 7,
            TestWeapon = 8,
            SemiAutoLegacy = 9
        }

        public enum AmmoType
        {
            Pouch = 0,
            Magazine = 1,
            AmmoLoader = 2,
            SemiAuto = 3,
            ShotgunShell = 4,
            Revolver = 5,
            Battery = 6,
            Sniper = 7,
            Explosive = 8,
            Generic = 9
        }

        public enum ProjectileType
        {
            notype = 0,
            Pierce = 1,
            Explosive = 2,
            Energy = 3,
            Blunt = 4,
            HitScan = 5,
            Sniper = 6
        }

        public enum AttachmentType
        {
            SecondaryFire = 0,
            Flashlight = 1,
            Laser = 2,
            GrenadeLauncher = 3,
            AmmoCounter = 4,
            Compass = 5,
            FireModeSwitch = 6
        }

        public static Array weaponTypeEnums = Enum.GetValues(typeof(WeaponType));

        public static Array ammoTypeEnums = Enum.GetValues(typeof(AmmoType));

        public static Array projectileTypeEnums = Enum.GetValues(typeof(ProjectileType));

        public static Array attachmentTypeEnums = Enum.GetValues(typeof(AttachmentType));

        /// <summary>
        /// Defines which behaviour should be produced at runtime
        /// </summary>
        public enum FireMode
            {
            /// <summary>
            /// Used for when the weapon is in safe-mode or is unable to fire for other reasons
            /// </summary>
            Safe = 0,
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
            int selectionIndex = (int)currentSelection;
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
        public static void ApplyRecoil(Rigidbody itemRB, float[] recoilForces, float recoilMult = 1.0f, bool leftHandHaptic = false, bool rightHandHaptic = false, float hapticForce = 1.0f)
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
            try {
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
                    if (creature == Player.currentCreature) continue;
                    if (Math.Abs(Vector3.Distance(creature.transform.position, origin)) <= blastRadius)
                    {
                        // Kill Creatures in Range
                        //Debug.Log("[F-L42-HitscanExplosion] Hit Creature: " + creature.name);
                        if (!creature.isKilled)
                        {
                            //Debug.Log("[F-L42-HitscanExplosion] Damaging Creature: " + creature.name);
                            creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Energy, 9999f), (MaterialData)null, (MaterialData)null));
                        }
                        // Apply Forces to Creature Main Body
                        creature.locomotion.rb.AddExplosionForce(force * creature.locomotion.rb.mass, origin, blastRadius, liftMult, forceMode);
                        creature.locomotion.rb.AddForce(Vector3.up * liftMult * creature.locomotion.rb.mass, forceMode);

                        //// Dismember Creature Parts
                        creature.ragdoll.headPart.Slice();
                        creature.ragdoll.GetPart(RagdollPart.Type.LeftLeg).Slice();
                        creature.ragdoll.GetPart(RagdollPart.Type.RightLeg).Slice();
                        creature.ragdoll.GetPart(RagdollPart.Type.RightArm).Slice();
                        creature.ragdoll.GetPart(RagdollPart.Type.LeftArm).Slice();


                        // Apply Forces to Creature Parts
                        foreach (RagdollPart part in creature.ragdoll.parts)
                        {
                            //Debug.Log("[F-L42-HitscanExplosion] Appyling Force to RD-part " + part.name);
                            part.rb.AddExplosionForce(force * part.rb.mass, origin, blastRadius, liftMult, forceMode);
                            part.rb.AddForce(Vector3.up * liftMult * part.rb.mass, forceMode);
                        }
                    }
                }
            }
            catch(Exception e) {
                Debug.LogError("[F-L42-HitscanExplosion][EXCEPTION] " + e.Message + " \n " + e.StackTrace);
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
        public static void SetFireSelectionAnimator(Animator animator, FireMode selection, string paramFloat1 = "x", string paramFloat2 = "y")
        {
            if (animator == null) return;
            try
            {
                animator.SetFloat(paramFloat1, blendTreePositions[(int)selection, 0]);
                animator.SetFloat(paramFloat2, blendTreePositions[(int)selection, 1]);
            }
            catch { Debug.LogError("[FL42-FirearmFunctions][SetSwitchAnimation] Exception in setting Animator floats 'x' and 'y'"); }
        }

        public static void RayCastShoot(Item sourceItem, Transform rayCastPoint, float maxDistance, float force)
        {
            Ray laserRay = new Ray(rayCastPoint.position, rayCastPoint.forward);

            if (Physics.Raycast(laserRay, out RaycastHit hit, maxDistance))
            {
                Creature hitCreature = hit.collider.transform.root.GetComponent<Creature>();
                if (hitCreature != null)
                {
                    Debug.Log("Hit Creature: " + hitCreature.name);

                    RagdollPart hitPart = hit.transform.GetComponentInChildren<RagdollPart>();

                    if (hitPart != null)
                    {
                        Debug.Log("Hit Part: " + hitPart.name);
                        hitCreature.locomotion.rb.AddForce(rayCastPoint.forward * force, ForceMode.Impulse);
                        hitPart.rb.AddForce(rayCastPoint.forward * force, ForceMode.Impulse);
                        

                        CollisionInstance thisCollision = new CollisionInstance(new DamageStruct(DamageType.Pierce, 99999f), (MaterialData) null, (MaterialData)null)
                        {
                            contactPoint = hit.collider.transform.position
                        };

                        EffectInstance thisEffect = new EffectInstance();
                        EffectData hitEffect = Catalog.GetData<EffectData>("HitBladeDecalFlesh", true);
                        thisEffect.AddEffect(hitEffect, hit.point, Quaternion.Euler(0f, 0f, 0f), hit.collider.transform, thisCollision, false);

                        hitEffect = Catalog.GetData<EffectData>("HitMaterialFlesh", true); 
                        thisEffect.AddEffect(hitEffect, hit.point, Quaternion.Euler(0f, 0f, 0f), hit.collider.transform, thisCollision, false);

                        hitEffect = Catalog.GetData<EffectData>("HitProjectileOnFlesh", true); 
                        thisEffect.AddEffect(hitEffect, hit.point, Quaternion.Euler(0f, 0f, 0f), hit.collider.transform, thisCollision, false);


                        hitEffect = Catalog.GetData<EffectData>("DropBlood", true);
                        thisEffect.AddEffect(hitEffect, hit.point, Quaternion.Euler(0f, 0f, 0f), hit.collider.transform, thisCollision, false);
                        thisEffect.Play();

                        hitCreature.Damage(thisCollision);

                        if ((hitPart.name.Contains("Arm") || hitPart.name.Contains("Hand") || hitPart.name.Contains("Leg") || hitPart.name.Contains("Head"))) hitPart.Slice();

                        hitCreature.TestKill();

                    }
                }
            }
            else
            {

            }
        }


       /// <summary>
       /// Dynamically sets Unity Collision Handling to ignore collisions between firearms and projectiles
       /// </summary>
       /// <param name="shooter"></param>
       /// <param name="i"></param>
       /// <param name="ignore"></param>
        public static void IgnoreProjectile(Item shooter, Item i, bool ignore = true)
        {
            foreach (ColliderGroup colliderGroup in shooter.colliderGroups)
            {
                foreach (Collider collider in colliderGroup.colliders)
                {
                    foreach (ColliderGroup colliderGroupProjectile in i.colliderGroups)
                    {
                        foreach (Collider colliderProjectile in colliderGroupProjectile.colliders)
                        {
                            Physics.IgnoreCollision(collider, colliderProjectile, ignore);
                        }
                    }
                    //Physics.IgnoreLayerCollision(collider.gameObject.layer, GameManager.GetLayer(LayerName.MovingObject));
                }
            }
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
        public static void ShootProjectile(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell = null, float forceMult = 1.0f, float throwMult = 1.0f, bool pooled = false, Collider IgnoreArg1 = null, SetSpawningStatusDelegate SetSpawnStatus = null)
        {
            if ((spawnPoint == null) || (String.IsNullOrEmpty(projectileID))) return;

            var projectileData = Catalog.GetData<ItemData>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                SetSpawnStatus?.Invoke(true);
                projectileData.SpawnAsync(i =>
                {
                    try
                    {
                        i.Throw(throwMult, Item.FlyDetection.Forced);
                        shooterItem.IgnoreObjectCollision(i);
                        i.IgnoreObjectCollision(shooterItem);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                        if (IgnoreArg1 != null)
                        {
                            try
                            {
                                i.IgnoreColliderCollision(IgnoreArg1);
                                foreach (ColliderGroup CG in shooterItem.colliderGroups)
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

                        IgnoreProjectile(shooterItem, i, true);

                        i.transform.position = spawnPoint.position;
                        i.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                        i.rb.velocity = shooterItem.rb.velocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * forceMult);

                        Projectiles.BasicProjectile projectileController = i.gameObject.GetComponent<Projectiles.BasicProjectile>();
                        if (projectileController != null) {
                            projectileController.SetShooterItem(shooterItem);
                            }

                        //-- Optional Switches --//
                        //i.rb.useGravity = false;
                        //i.SetColliderAndMeshLayer(GameManager.GetLayer(LayerName.Default));
                        //i.SetColliderLayer(GameManager.GetLayer(LayerName.None));
                        //i.ignoredItem = shooterItem;
                        //shooterItem.IgnoreObjectCollision(i);
                        //Physics.IgnoreLayerCollision(GameManager.GetLayer(LayerName.None), GameManager.GetLayer(LayerName.Default));

                        SetSpawnStatus?.Invoke(false);
                        if (!String.IsNullOrEmpty(imbueSpell))
                        {
                            // Set imbue charge on projectile using ItemProjectileSimple subclass
                            //Projectiles.SimpleProjectile projectileController = i.gameObject.GetComponent<Projectiles.SimpleProjectile>();
                            if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("[Fisher-Firearms] EXCEPTION IN SPAWNING " + ex.Message + " \n " + ex.StackTrace);
                    }
                },
                Vector3.zero,
                Quaternion.Euler(Vector3.zero),
                null,
                false);
            }
        }

        public static void ProjectileBurst(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell = null, float forceMult = 1.0f, float throwMult = 1.0f, bool pooled = false, Collider IgnoreArg1 = null)
        {
            var projectileData = Catalog.GetData<ItemData>(projectileID, true);
            if ((spawnPoint == null) || (String.IsNullOrEmpty(projectileID))) return;
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            foreach (Vector3 offsetVec in buckshotOffsetPosiitions)
            {

                projectileData.SpawnAsync(i =>
                {
                    try
                    {
                        i.transform.position = spawnPoint.position + offsetVec;
                        i.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                        i.rb.velocity = shooterItem.rb.velocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * forceMult);
                        shooterItem.IgnoreObjectCollision(i);
                        i.IgnoreObjectCollision(shooterItem);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);

                        if (IgnoreArg1 != null)
                        {
                            try
                            {
                                i.IgnoreColliderCollision(IgnoreArg1);
                                foreach (ColliderGroup CG in shooterItem.colliderGroups)
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
                            projectileController.SetShooterItem(shooterItem);
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
                        Debug.Log("[Fisher-Firearms] EXCEPTION IN SPAWNING " + ex.Message + " \n " + ex.StackTrace);
                    }
                },
                spawnPoint.position,
                Quaternion.Euler(spawnPoint.rotation.eulerAngles),
                null,
                false);

            }

        }

        public static void ShotgunBlast(Item shooterItem, string projectileID, Transform spawnPoint, float distance, float force, float forceMult, string imbueSpell = null, float throwMult = 1.0f, bool pooled = false, Collider IgnoreArg1 = null)
        {
            if (Physics.Raycast(spawnPoint.position, spawnPoint.forward, out RaycastHit hit, distance))
            {
                Creature hitCreature = hit.collider.transform.root.GetComponentInParent<Creature>();
                if (hitCreature != null)
                {
                    if (hitCreature == Player.currentCreature) return;
                    //Debug.Log("[FL42 - FirearmFunctions][hitCreature] Hit creature!");
                    hitCreature.locomotion.rb.AddExplosionForce(force, hit.point, 1.0f, 1.0f, ForceMode.VelocityChange);
                    //hitCreature.ragdoll.SetState(Creature.State.Destabilized);
                    foreach (RagdollPart part in hitCreature.ragdoll.parts)
                    {
                        part.rb.AddExplosionForce(force, hit.point, 1.0f, 1.0f, ForceMode.VelocityChange);
                        part.rb.AddForce(spawnPoint.forward * force, ForceMode.Impulse);
                    }
                }
                else
                {
                    try
                    {
                        //Debug.Log("[FL42 - FirearmFunctions][hitCreature] Hit item");
                        hit.collider.attachedRigidbody.AddExplosionForce(force, hit.point, 0.5f, 1.0f, ForceMode.VelocityChange);
                        hit.collider.attachedRigidbody.AddForce(spawnPoint.forward * force, ForceMode.Impulse);
                    }
                    catch { }

                }

            }

            var projectileData = Catalog.GetData<ItemData>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            foreach (Vector3 offsetVec in buckshotOffsetPosiitions)
            {
                projectileData.SpawnAsync(i =>
                {
                    try
                    {
                        i.transform.position = spawnPoint.position + offsetVec;
                        i.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                        i.rb.velocity = shooterItem.rb.velocity;
                        i.rb.AddForce(i.rb.transform.forward * 1000.0f * forceMult);
                        shooterItem.IgnoreObjectCollision(i);
                        i.IgnoreObjectCollision(shooterItem);
                        i.IgnoreRagdollCollision(Player.local.creature.ragdoll);

                        if (IgnoreArg1 != null)
                        {
                            try
                            {
                                i.IgnoreColliderCollision(IgnoreArg1);
                                foreach (ColliderGroup CG in shooterItem.colliderGroups)
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
                            projectileController.SetShooterItem(shooterItem);
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
                        Debug.Log("[Fisher-Firearms] EXCEPTION IN SPAWNING " + ex.Message + " \n " + ex.StackTrace);
                    }
                },
                spawnPoint.position,
                Quaternion.Euler(spawnPoint.rotation.eulerAngles),
                null,
                false);

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
        public static IEnumerator GeneralFire(TrackFiredDelegate TrackedFire, TriggerPressedDelegate TriggerPressed, FireMode fireSelector = FireMode.Single, int fireRate = 60, int burstNumber = 3, AudioSource emptySoundDriver = null, IsFiringDelegate WeaponIsFiring = null, IsSpawningDelegate ProjectileIsSpawning = null)
        {
            WeaponIsFiring?.Invoke(true);
            float fireDelay = 60.0f / (float)fireRate;

            if (fireSelector == FireMode.Safe)
            {
                if (emptySoundDriver != null) emptySoundDriver.Play();
                yield return null;
            }

            else if (fireSelector == FireMode.Single)
            {
                if (ProjectileIsSpawning != null)
                {
                    do yield return null;
                    while (ProjectileIsSpawning());
                }

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

                    if (ProjectileIsSpawning != null)
                    {
                        do yield return null;
                        while (ProjectileIsSpawning());
                    }

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
                    if (ProjectileIsSpawning != null)
                    {
                        do yield return null;
                        while (ProjectileIsSpawning());
                    }

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


        public static void DamageCreatureCustom(Creature triggerCreature, float damageApplied, Vector3 hitPoint)
        {
            try
            {
                if (triggerCreature.currentHealth > 0)
                {
                    //Debug.Log("[F-L42-RayCast] Damaging enemy: " + triggerCreature.name);
                    //Debug.Log("[F-L42-RayCast] Setting MaterialData... ");
                    MaterialData sourceMaterial = Catalog.GetData<MaterialData>("Metal", true); 
                    MaterialData targetMaterial = Catalog.GetData<MaterialData>("Flesh", true);

                    DamageStruct damageStruct = new DamageStruct(DamageType.Pierce, damageApplied);

                    //Debug.Log("[F-L42-RayCast] Defining CollisionStruct... ");
                    CollisionInstance collisionStruct = new CollisionInstance(damageStruct, (MaterialData)sourceMaterial, (MaterialData)targetMaterial)
                    {
                        contactPoint = hitPoint
                    };
                    //Debug.Log("[F-L42-RayCast] Applying Damage to creature... ");
                    triggerCreature.Damage(collisionStruct);
                    //Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

                    //Debug.Log("[F-L42-RayCast] SpawnEffect... ");
                    if (collisionStruct.SpawnEffect(sourceMaterial, targetMaterial, false, out EffectInstance effectInstance))
                    {
                        effectInstance.Play();
                    }
                    //Debug.Log("[F-L42-RayCastFire] Damage Applied: " + damageApplied);

                }
            }
            catch
            {
                //Debug.Log("[F-L42-RayCast][ERROR] Unable to damage enemy!");
            }
        }

        public static void DumpRigidbodyToLog(Rigidbody rb)
        {
            Debug.LogWarning("[Fisher-Firearms][RB-DUMP] " + rb.name + ": " + rb.ToString());
            Debug.LogWarning("[Fisher-Firearms][RB-DUMP] Name: " + rb.name + "| Mass: " + rb.mass + "| Kinematic: " + rb.isKinematic.ToString() + "| Gravity: " + rb.useGravity.ToString() + "| Interpolation: " + rb.interpolation.ToString() + "| Detection: " + rb.collisionDetectionMode.ToString());
        }

    }
}
