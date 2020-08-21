using UnityEngine;
using ThunderRoad;
using System;
using System.Collections;

namespace ModularFirearms
{
    public class ItemProjectileSimple : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleProjectileSimple module;
        protected float flyDelay = 1.0f;
        private string spellQueue;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleProjectileSimple>();
            if (module.allowFlyTime) item.rb.useGravity = false;
        }

        protected void Start()
        {
            StartCoroutine(LifetimeGenerator(module.lifetime, module.flyDelay));
        }

        public void AddChargeToQueue(string SpellID)
        {
            spellQueue = SpellID;
        }

        private void OnCollisionEnter(Collision hit)
        {
            if (item.rb.useGravity) return;
            item.rb.useGravity = true;
        }

        private void TransferImbueCharge(Item imbueTarget, string spellQueue)
        {
            if (!String.IsNullOrEmpty(spellQueue) && imbueTarget.isActiveAndEnabled)
            {
                SpellCastCharge transferedSpell = Catalog.GetData<SpellCastCharge>(spellQueue, true).Clone();
                foreach (Imbue itemImbue in imbueTarget.imbues)
                {
                    if (string.IsNullOrEmpty(spellQueue)) return;
                    try { StartCoroutine(FirearmFunctions.TransferDeltaEnergy(itemImbue, transferedSpell)); }
                    catch { }
                    spellQueue = null;
                }
            }
        }

        private IEnumerator LifetimeGenerator(float lifetime = 1.0f, float flyDelay = 1.0f)
        {
            TransferImbueCharge(item, spellQueue);
            if (!item.rb.useGravity)
            {
                yield return new WaitForSeconds(flyDelay);
                item.rb.useGravity = true;
            }
            yield return new WaitForSeconds(lifetime);
            item.Despawn();
            yield return null;
        }

    }
}
