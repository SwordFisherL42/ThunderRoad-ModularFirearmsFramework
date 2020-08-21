using UnityEngine;
using ThunderRoad;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ModularFirearms
{
    public class ItemMagazine : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleMagazine module;
        protected ObjectHolder holder;
        protected Handle magazineHandle;
        protected GameObject bulletMesh;
        protected int ammoCount;
        protected bool insertedIntoObject = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleMagazine>();

            holder = item.GetComponentInChildren<ObjectHolder>();
            holder.Snapped += new ObjectHolder.HolderDelegate(this.OnAmmoItemInserted);

            magazineHandle = item.definition.GetCustomReference(module.handleRef).GetComponent<Handle>();
            bulletMesh = item.definition.GetCustomReference(module.bulletMeshRef).gameObject;
            RefillAll();
        }

        public void OnAmmoItemInserted(Item interactiveObject)
        {
            try
            {
                ItemAmmo addedAmmo = interactiveObject.GetComponent<ItemAmmo>();
                if (addedAmmo != null)
                {
                    if (addedAmmo.GetAmmoType() == module.acceptedAmmoType)
                    {
                        RefillOne();
                        holder.UnSnap(interactiveObject);
                        interactiveObject.Despawn();
                        return;
                    }
                }
                else
                {
                    holder.UnSnap(interactiveObject);
                    Debug.LogWarning("[Fisher-Firearms][WARNING] Inserted object has no ItemAmmo component, and will be popped out");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Fisher-Firearms][ERROR] Exception in Adding Ammo.");
                Debug.LogError(e.ToString());
            }
            return;
        }

        public void Insert() {
            insertedIntoObject = true;
            magazineHandle.data.disableTouch = true;
        }

        public void Eject() {
            item.rb.AddRelativeForce(new Vector3(module.ejectionForceVector[0], module.ejectionForceVector[1], module.ejectionForceVector[2]), ForceMode.Impulse);
            insertedIntoObject = false;
            magazineHandle.data.disableTouch = false;
        }

        public void ConsumeOne()
        {
            ammoCount -= 1;
            if (ammoCount <= 0)
            {
                SetBulletVisibility(false);
            }
            return;
        }

        public void ConsumeAll()
        {
            ammoCount = 0;
            SetBulletVisibility(false);
            return;
        }

        public void RefillOne()
        {
            if (ammoCount <= 0)
            {
                SetBulletVisibility(true);
            }
            ammoCount += 1;
            return;
        }

        public void RefillAll()
        {
            ammoCount = module.ammoCapacity;
            SetBulletVisibility(true);
            return;
        }

        public void SetBulletVisibility(bool visible = true)
        {
            bulletMesh.SetActive(visible);
        }

        public int GetAmmoCount()
        {
            return ammoCount;
        }

        public string GetMagazineID()
        {
            return item.data.id;
        }

        public bool IsInserted()
        {
            return insertedIntoObject;
        }
    }
}
