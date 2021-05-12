using System;
using ThunderRoad;
using UnityEngine;

namespace ShotgunShellHolder
{
    class BulletHolderSpawner : MonoBehaviour
    {
        protected Item item;
        protected BulletHolderModule module;

        private Holder bulletHolder;

        void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<BulletHolderModule>();
            if (!String.IsNullOrEmpty(module.holderRef)) bulletHolder = item.GetCustomReference(module.holderRef).GetComponent<Holder>();

        }

        void Start()
        {
            foreach (Transform _ in bulletHolder.slots)
            {
                SpawnAndSnap(module.ammoID);
            }

        }

        private void SpawnAndSnap(string ammoID)
        {
            var ammoData = Catalog.GetData<ItemData>(ammoID, true);
            if (ammoData == null)
            {
                Debug.LogError("[Fisher-BulletHolderSpawner][ERROR] No Ammo named " + ammoID.ToString());
                return;
            }
            else
            {
                ammoData.SpawnAsync(i =>
                {
                    try
                    {
                        bulletHolder.Snap(i);
                    }
                    catch
                    {
                        Debug.Log("[Fisher-BulletHolderSpawner] EXCEPTION IN SNAPPING AMMO ");
                    }
                },
                item.transform.position,
                Quaternion.Euler(item.transform.rotation.eulerAngles),
                null,
                false);
            }
        }
    }
}
