using System;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class AmmoCounterController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        /// Ammo Display Controller ///
        private TextureProcessor ammoCounter;
        private MeshRenderer ammoCounterMesh;
        private Texture2D digitsGridTexture;
        private int lastAmmoCount = 0;
        private int newAmmoCount = 0;
        /// Parent Class Controller ///
        private Weapons.BaseFirearmGenerator parentFirearm;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            parentFirearm = this.GetComponent<Weapons.BaseFirearmGenerator>();
            if (!String.IsNullOrEmpty(module.ammoCounterRef))
            {
                ammoCounterMesh = item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
                digitsGridTexture = (Texture2D)item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
            }
            if ((digitsGridTexture != null) && (ammoCounterMesh != null))
            {
                ammoCounter = new TextureProcessor();
                ammoCounter.SetGridTexture(digitsGridTexture);
                ammoCounter.SetTargetRenderer(ammoCounterMesh);
            }
            if (ammoCounter != null) ammoCounter.DisplayUpdate(newAmmoCount);
        }

        public void LateUpdate()
        {
            newAmmoCount = parentFirearm.GetAmmoCounter();
            if (lastAmmoCount != newAmmoCount)
            {
                ammoCounter.DisplayUpdate(newAmmoCount);
                lastAmmoCount = newAmmoCount;
            }
        }
    }
}
