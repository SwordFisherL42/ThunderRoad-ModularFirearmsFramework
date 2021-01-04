using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    // Modular quick loader class. When an object with this module is inserted, all quiver projectiles are respawned based on `module.bulletCount`
    class ItemAmmoLoader : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleAmmoLoader module;
        protected MeshRenderer bulletMesh;
        public bool isLoaded = true;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleAmmoLoader>();
            bulletMesh = item.GetCustomReference(module.bulletMeshID).GetComponent<MeshRenderer>();
        }

        public bool ConsumeAmmo()
        {
            ToggleMeshState(bulletMesh);
            isLoaded = bulletMesh.enabled;
            return isLoaded;
        }

        public int CountBullets()
        {
            return module.bulletCount;
        }

        protected void ToggleMeshState(MeshRenderer ammoMesh)
        {
            ammoMesh.enabled = !ammoMesh.enabled;
            return;
        }

    }
}
