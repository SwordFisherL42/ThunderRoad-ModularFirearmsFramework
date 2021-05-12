using System;
using UnityEngine;
using ThunderRoad;

/* Description: An Item plugin for `ThunderRoad` which is required on any items
 * that are set up as a projectile. This class allows projectiles to be imbued 
 * via the AddChargeToQueue(...) method and defines an item lifetime for performance.
 * 
 * author: SwordFisherL42 ("Fisher")
 * 
 */

namespace ModularFirearms.Projectiles
{
    public class SimpleProjectile : MonoBehaviour
    {
        protected Item item;
        protected Shared.ProjectileModule module;
        protected string queuedSpell;
        protected bool isFlying = false;
        private Creature hitCreature;
        private RagdollPart hitPart;
        private MaterialData bladeMaterial;
        private MaterialData fleshMaterial;
        private CollisionInstance thisCollision;
        private EffectInstance thisEffect;
        private ParticleSystem SplatterEffect;
        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.ProjectileModule>();
            if (!string.IsNullOrEmpty(module.CustomSplatterEffect)) SplatterEffect = item.GetCustomReference(module.CustomSplatterEffect).GetComponent<ParticleSystem>();
            bladeMaterial = Catalog.GetData<MaterialData>("Blade", true);
            fleshMaterial = Catalog.GetData<MaterialData>("Flesh", true);
        }

        protected void Start()
        {
            if (module.allowFlyTime) { item.rb.useGravity = false; isFlying = true; }
            item.Despawn(module.lifetime);
            item.isThrowed = true;
        }

        public void AddChargeToQueue(string SpellID)
        {
            queuedSpell = SpellID;
        }

        private void LateUpdate()
        {
            if (isFlying) item.rb.velocity = item.rb.velocity * module.flyingAcceleration;
            TransferImbueCharge(item, queuedSpell);

            
            //if (item.isPenetrating)
            //{

            //}
        }

        private void DamageCreatureCustom(Creature hitCreature, RagdollPart hitPart)
        {
            if ((hitCreature == null) || (hitPart == null)) return;

        }

        private void OnCollisionEnter(Collision hit)
        {
            hitCreature = hit.transform.root.GetComponentInChildren<Creature>();
            if (hitCreature != null)
            {
                Debug.Log("Hit Creature: " + hitCreature.name);
                Debug.Log("Hit Collider: " + hit.collider.name);
                Debug.Log("Hit Transform: " + hit.transform.name);
                Debug.Log("Hit GameObject: " + hit.gameObject.name);
                Debug.Log(string.Format("[ON HIT] Creature Health:{0} isKilled:{1} isPooled:{2}", hitCreature.currentHealth, hitCreature.isKilled, hitCreature.pooled));
                hitPart = hit.transform.GetComponentInChildren<RagdollPart>();
                
                if (hitPart != null)
                {
                    Debug.Log("Hit Part: " + hitPart.name);

                    thisCollision = new CollisionInstance(new DamageStruct(DamageType.Pierce, 99999f), (MaterialData)bladeMaterial, (MaterialData)fleshMaterial)
                    {
                        contactPoint = hit.collider.transform.position,
                        contactNormal = Quaternion.LookRotation(Player.local.head.transform.position).eulerAngles
                    };

                    foreach (CollisionHandler collisionHandler in item.collisionHandlers)
                    {
                        foreach (Damager damager1 in collisionHandler.damagers)
                        {
                            Debug.Log("Sending Penetrate on damager: " + damager1.name);
                            damager1.Penetrate(thisCollision, false);
                        }

                    }
                    Debug.Log(string.Format("[AFTER PENETRATE] Creature Health:{0} isKilled:{1} isPooled:{2}", hitCreature.currentHealth, hitCreature.isKilled, hitCreature.pooled));

                    Debug.Log("Removing Creature Pool Status");
                    hitCreature.pooled = false;

                    Debug.Log("Damaging Creature with collision....");
                    hitCreature.Damage(thisCollision);
                    Debug.Log(string.Format("[AFTER DAMAGE] Creature Health:{0} isKilled:{1} isPooled:{2}", hitCreature.currentHealth, hitCreature.isKilled, hitCreature.pooled));

                    //if (SplatterEffect != null)
                    //{
                    //    Debug.Log("Playing Splatter Effect");
                    //    SplatterEffect.transform.parent = hitPart.transform;
                    //    SplatterEffect.Play();
                    //}
                    //hitCreature.pooled = false;
                    //hitCreature.Kill();
                    //hitCreature.Despawn(5.0f);
                    //item.Despawn(0.5f);
                    //hitCreature.locomotion.rb.AddExplosionForce(10.0f, hitCreature.transform.position, 1.5f, 1.0f, ForceMode.Impulse);
                    //hitPart.rb.AddExplosionForce(10.0f, hitCreature.transform.position, 1.5f, 1.0f, ForceMode.Impulse);
                    //if ((hitPart.name.Contains("Arm") || hitPart.name.Contains("Hand") || hitPart.name.Contains("Leg"))) hitPart.Slice();
                    //thisCollision = new CollisionInstance(new DamageStruct(DamageType.Pierce, 99999f), (MaterialData)bladeMaterial, (MaterialData)fleshMaterial)
                    //{
                    //    contactPoint = hit.collider.transform.position
                    //};
                    //thisCollision.SpawnEffect(bladeMaterial, fleshMaterial, false, out thisEffect);
                    //thisCollision.NewHit(item.colliderGroups[0].colliders[0], hit.collider, hit.collider.transform.GetComponent<ColliderGroup>(), item.colliderGroups[0], item.transform.forward * 25.0f, hit.contacts[0].point, hit.contacts[0].normal, 2.0f, bladeMaterial, fleshMaterial);
                    //hitCreature.Damage(thisCollision);

                    ////hitCreature.ragdoll.SetState(Ragdoll.State.Inert);
                    ////hitCreature.isKilled = true;


                }
            }

            if (item.rb.useGravity) return;
            else { item.rb.useGravity = true; isFlying = false;
                // Debug.Log("[PROJECTILE] Collsion with: " + hit.collider.name);
            }
        }

        private void TransferImbueCharge(Item imbueTarget, string spellID)
        {
            if (String.IsNullOrEmpty(spellID)) return;
            SpellCastCharge transferedSpell = Catalog.GetData<SpellCastCharge>(spellID, true).Clone();
            foreach (Imbue itemImbue in imbueTarget.imbues)
            {
                try
                {
                    StartCoroutine(FirearmFunctions.TransferDeltaEnergy(itemImbue, transferedSpell));
                    queuedSpell = null;
                    return;
                }
                catch { }
            }
        }

    }
}
