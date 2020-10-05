using UnityEngine;
using ThunderRoad;
using System;

namespace ModularFirearms
{
    public class ItemSimpleProjectile : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleSimpleProjectile module;
        protected string queuedSpell;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleSimpleProjectile>();
        }

        protected void Start()
        {
            if (module.allowFlyTime) item.rb.useGravity = false;
            if (module.lifetime > 0.0f)  item.Despawn(module.lifetime);
        }

        public void AddChargeToQueue(string SpellID)
        {
            queuedSpell = SpellID;
        }

        private void LateUpdate()
        {
            TransferImbueCharge(item, queuedSpell);
        }

        private void OnCollisionEnter(Collision hit)
        {
            if (item.rb.useGravity) return;
            else item.rb.useGravity = true;
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
