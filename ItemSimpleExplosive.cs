using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms
{
    public class ItemSimpleExplosive : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleSimpleExplosive module;
        private ParticleSystem explosiveEffect;
        private AudioSource explosiveSound;
        private GameObject meshObject;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleSimpleExplosive>();
            meshObject = item.GetCustomReference(module.shellMeshRef).gameObject;
            if (!string.IsNullOrEmpty(module.particleEffectRef)) explosiveEffect = item.GetCustomReference(module.particleEffectRef).GetComponent<ParticleSystem>();
            if (!string.IsNullOrEmpty(module.soundRef)) explosiveSound = item.GetCustomReference(module.soundRef).GetComponent<AudioSource>();
        }

        protected void Start()
        {
            item.Despawn(module.lifetime);  //Default despawn, if no collisions occur
        }

        public void IgnoreItem(Item interactiveObject)
        {
            item.IgnoreObjectCollision(interactiveObject);
        }

        private void Explode()
        {
            explosiveSound.transform.parent = null;
            explosiveSound.Play();
            meshObject.SetActive(false);
            if (explosiveEffect != null)
            {
                explosiveEffect.transform.parent = null;
                HitscanExplosion(explosiveEffect.transform.position, module.explosiveForce, module.blastRadius, module.liftMult, (ForceMode)forceModeEnums.GetValue(module.forceMode));
                explosiveEffect.Play();
            }
            
        }

        private void OnCollisionEnter(Collision hit)
        {
            //Debug.Log("[F-L42] COLLISON WITH " + hit.transform.name);
            Explode();
            item.Despawn();
        }

    }
}
