using UnityEngine;
using BS;

namespace FishersFirearmsModular
{
    // Modular ammunition class, self-tracks if it is consumed (`ready to fire` state).
    class ItemAmmo : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleAmmo module;
        protected MeshRenderer bulletMesh;
        protected Handle ammoHandle;
        public bool isLoaded = true;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleAmmo>();
            if (module.handleRef != null) ammoHandle = item.definition.GetCustomReference(module.handleRef).GetComponent<Handle>();
            bulletMesh = item.definition.GetCustomReference(module.bulletMeshID).GetComponent<MeshRenderer>();
        }

        public int GetAmmoType()
        {
            return module.ammoType;
        }

        public void Consume()
        {
            SetMeshState(bulletMesh);
            isLoaded = false;
            if (ammoHandle != null)
            {
                ammoHandle.data.allowedTeleGrab = Handle.AllowedGrab.ObjectGrabbed;
            }
            return;
        }

        public void Refill()
        {
            SetMeshState(bulletMesh, true);
            isLoaded = true;
            if (ammoHandle != null)
            {
                ammoHandle.data.allowedTeleGrab = Handle.AllowedGrab.Dropped;
            }
            return;
        }

        protected void SetMeshState(MeshRenderer ammoMesh, bool newState = false)
        {
            ammoMesh.enabled = newState;
            return;
        }

    }
}
