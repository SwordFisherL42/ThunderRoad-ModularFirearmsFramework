using UnityEngine;
using ThunderRoad;
using System;

namespace ModularFirearms.Items
{
    public class AmmoResupply : MonoBehaviour
    {
        protected Item item;
        protected Shared.AmmoModule module;
        protected Holder holder;

        private bool infiniteUses = false;
        private int usesRemaining = 0;
        private bool waitingForSpawn = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AmmoModule>();
            holder = item.GetComponentInChildren<Holder>();
            holder.UnSnapped += new Holder.HolderDelegate(this.OnWeaponItemRemoved);
            if (module.ammoCapacity > 0) { usesRemaining = module.ammoCapacity; infiniteUses = false; }
            else { infiniteUses = true; }
            return;
        }

        protected void Start()
        {
            // Spawn initial random item in the holder
            SpawnAndSnap(module.magazineID, holder);
        }

        protected void SpawnAndSnap(string spawnedItemID, Holder holder)
        {
            if (waitingForSpawn) return;
            ItemData spawnedItemData = Catalog.GetData<ItemData>(spawnedItemID, true);
            if (spawnedItemData == null) return;
            else
            {
                waitingForSpawn = true;
                spawnedItemData.SpawnAsync(thisSpawnedItem =>
                {
                    //Debug.Log("[ModularFirearmsFramework] Time: " + Time.time + " Spawning weapon: " + thisSpawnedItem.name);
                    try
                    {
                        if (holder.HasSlotFree())
                        {
                            holder.Snap(thisSpawnedItem);
                            thisSpawnedItem.SetMeshLayer(GameManager.GetLayer(LayerName.FPVHide));
                            //Debug.Log("[ModularFirearmsFramework] Time: " + Time.time + " Snapped weapon: " + thisSpawnedItem.name);
                        }
                        else
                        {
                            //Debug.Log("[ModularFirearmsFramework] EXCEPTION Time: " + Time.time + " NO FREE SLOT FOR: " + thisSpawnedItem.name);
                        }
                        waitingForSpawn = false;
                    }
                    catch (Exception e) { Debug.Log("[ModularFirearmsFramework] EXCEPTION IN SNAPPING: " + e.ToString()); }
                });
                //Debug.Log("[Fisher-HoldingBags] Time: " + Time.time + " Activating SpawnAndSnap: " + spawnedItemID);
                return;
            }
        }

        protected void OnWeaponItemRemoved(Item interactiveObject)
        {
            if (waitingForSpawn) return;

            if ((!infiniteUses) && (usesRemaining <= 0))
            {
                holder.data.locked = true;
                if (module.despawnBagOnEmpty) item.Despawn();
                return;
            }
            else
            {
                // Debug.Log("[Fisher-HoldingBags] Time: " + Time.time + " Activating OnWeaponItemRemoved: " + interactiveObject.data.id);
                SpawnAndSnap(module.magazineID, holder);
                usesRemaining -= 1;
                return;
            }
        }

    }
}
