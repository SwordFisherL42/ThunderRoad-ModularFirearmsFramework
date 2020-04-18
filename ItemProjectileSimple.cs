using UnityEngine;
using BS;

namespace FishersFirearmsModular
{
    // Simplified projectile class which enables a minimal template for projectile items.
    public class ItemProjectileSimple : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleProjectileSimple module;
        protected float lifetime;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleProjectileSimple>();
            lifetime = module.lifetime;
        }

        protected void FixedUpdate()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0) item.Despawn();
            return;
        }

    }
}
