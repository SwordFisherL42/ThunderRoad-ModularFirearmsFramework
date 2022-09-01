using System;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FrameworkCore;

namespace ModularFirearms.Projectiles
{
    public class ExplosiveProjectile : MonoBehaviour
    {
        protected Item item;
        protected Shared.ProjectileModule module;
        private ParticleSystem explosiveEffect;
        private AudioSource explosiveSound;
        private GameObject meshObject;
        protected bool isFlying = false;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.ProjectileModule>();
            if (!string.IsNullOrEmpty(module.shellMeshRef)) meshObject = item.GetCustomReference(module.shellMeshRef).gameObject;
            if (!string.IsNullOrEmpty(module.particleEffectRef)) explosiveEffect = item.GetCustomReference(module.particleEffectRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.soundRef)) explosiveSound = item.GetCustomReference(module.soundRef).GetComponent<AudioSource>();
        }

        protected void Start()
        {
            this.item.Throw(module.throwMult, Item.FlyDetection.Forced);
            if (module.allowFlyTime) { item.rb.useGravity = false; isFlying = true; }
            item.Despawn(module.lifetime);  //Default despawn, if no collisions occur
        }

        private void LateUpdate() {
            if (isFlying) item.rb.velocity = item.rb.velocity * module.flyingAcceleration;
        }

        public void IgnoreItem(Item interactiveObject)
        {
            item.IgnoreObjectCollision(interactiveObject);
        }

        private void Explode()
        {
            if (explosiveSound != null)
            {
                explosiveSound.transform.parent = null;
                explosiveSound.Play();
            }
            if (meshObject != null) meshObject.SetActive(false);
            if (explosiveEffect != null)
            {
                explosiveEffect.transform.parent = null;
                HitscanExplosion(explosiveEffect.transform.position, module.explosiveForce, module.blastRadius, module.liftMult, (ForceMode)Enum.Parse(typeof(ForceMode), module.forceMode));
                explosiveEffect.Play();
            }
        }

        private void OnCollisionEnter(Collision hit)
        {
            if (!item.rb.useGravity) { item.rb.useGravity = true; isFlying = false; }
            #if DEBUG
            Debug.Log("[ModularFirearmsFramework] COLLISON WITH " + hit.transform.name);
            #endif
            Explode();
            item.Despawn();
        }

    }
}
