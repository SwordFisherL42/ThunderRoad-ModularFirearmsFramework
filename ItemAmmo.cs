using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    // Modular ammunition class, self-tracks if it is consumed (`ready to fire` state).
    public class ItemAmmo : MonoBehaviour
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
            if (module.handleRef != null) ammoHandle = item.GetCustomReference(module.handleRef).GetComponent<Handle>();
            if (module.bulletMeshID != null) bulletMesh = item.GetCustomReference(module.bulletMeshID).GetComponent<MeshRenderer>();
            Refill();
        }

        public int GetAmmoType()
        {
            return module.ammoType;
        }

        public void Consume()
        {
            SetMeshState(bulletMesh);
            isLoaded = false;
            if (ammoHandle != null) ammoHandle.data.allowTelekinesis = false;
        }

        public void Refill()
        {
            SetMeshState(bulletMesh, true);
            isLoaded = true;
            if (ammoHandle != null) ammoHandle.data.allowTelekinesis = true;
            return;
        }

        protected void SetMeshState(MeshRenderer ammoMesh, bool newState = false)
        {
            if (ammoMesh != null) { ammoMesh.enabled = newState; }
            
            return;
        }

    }
}
