using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Common
{
    public class ModularAmmo : MonoBehaviour
    {
        protected Item item;
        protected AmmoModule module;
        protected MeshRenderer bulletMesh;
        protected Handle ammoHandle;
        public bool isLoaded = true;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<AmmoModule>();
            if (module.handleRef != null) ammoHandle = item.definition.GetCustomReference(module.handleRef).GetComponent<Handle>();
            if (module.bulletMeshID != null) bulletMesh = item.definition.GetCustomReference(module.bulletMeshID).GetComponent<MeshRenderer>();
            Refill();
        }

        public int GetAmmoType()
        {
            return module.ammoType;
        }

        public string GetAmmoID()
        {
            return item.data.id;
        }

        public int GetAmmoCount()
        {
            return module.numberOfRounds;
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
