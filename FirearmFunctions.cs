using UnityEngine;
using ThunderRoad;
using System;
using System.Collections;

// Core Framework Functions //

namespace ModularFirearms
{
    public class FirearmFunctions
    {
        public enum FireMode
        {
            Misfire = 0,
            Single = 1,
            Burst = 2,
            Auto = 3
        }

        public static FireMode CycleFireMode(FireMode currentSelection)
        {
            int selectionIndex = (int) currentSelection;
            selectionIndex++;
            Array fireModeEnums = Enum.GetValues(typeof(FireMode));
            if (selectionIndex >= fireModeEnums.Length) return (FireMode)fireModeEnums.GetValue(0);
            return (FireMode)fireModeEnums.GetValue(selectionIndex);
        }

        public static void Animate(Animator animator, string animationName)
        {
            if ((animator == null) || String.IsNullOrEmpty(animationName)) return;
            animator.Play(animationName);
        }

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

        public static void ShootProjectile(Item shooterItem, string projectileID, Transform spawnPoint, string imbueSpell=null, float forceMult=1.0f, float throwMult=1.0f)
        {
            var projectileData = Catalog.GetData<ItemPhysic>(projectileID, true);
            if (projectileData == null)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] No projectile named " + projectileID.ToString());
                return;
            }
            else
            {
                Item projectile = projectileData.Spawn(false);
                if (!projectile.gameObject.activeInHierarchy) projectile.gameObject.SetActive(true);
                shooterItem.IgnoreObjectCollision(projectile);
                // Set imbue charge on projectile using ItemProjectileSimple subclass
                ItemProjectileSimple projectileController = projectile.gameObject.GetComponent<ItemProjectileSimple>();
                if (projectileController != null) projectileController.AddChargeToQueue(imbueSpell);
                // Match the Position, Rotation, & Speed of the spawner item
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles);
                projectile.rb.velocity = shooterItem.rb.velocity;
                projectile.rb.AddForce(projectile.rb.transform.forward * 1000.0f * forceMult);
                projectile.Throw(throwMult, Item.FlyDetection.CheckAngle);
            }
        }

        public static string GetItemSpellChargeID(Item interactiveObject)
        {
            string itemSpellID = "";
            foreach (Imbue itemImbue in interactiveObject.imbues)
            {
                if (itemImbue.spellCastBase != null)
                {
                    itemSpellID = itemImbue.spellCastBase.id;
                }
            }
            return itemSpellID;
        }

        public static IEnumerator TransferDeltaEnergy(Imbue itemImbue, SpellCastCharge activeSpell, float energyDelta = 5.0f, int counts = 20)
        {
            for (int i = 0; i < counts; i++)
            {
                try { itemImbue.Transfer(activeSpell, energyDelta); }
                catch { }
                yield return new WaitForSeconds(0.01f);
            }
            yield return null;
        }
    }
}
