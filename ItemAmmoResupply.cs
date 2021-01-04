using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    // Based on `Infinite Quiver` code by spudjb
    class ItemAmmoResupply : MonoBehaviour
    {
        protected Item item;
        protected ItemQuiver itemQuiver;
        protected ItemModuleQuiver module;
        protected Holder holder;

        protected void Awake()
        {
            this.item = this.GetComponent<Item>();
            this.itemQuiver = this.GetComponent<ItemQuiver>();
            this.module = this.item.data.GetModule<ItemModuleQuiver>();
            this.holder = this.GetComponentInChildren<Holder>();
            this.holder.UnSnapped += new Holder.HolderDelegate(this.OnProjectileRemoved);
        }

        protected void OnProjectileRemoved(Item interactiveObject)
        {
            itemQuiver.SpawnAllProjectiles();
        }

    }
}
