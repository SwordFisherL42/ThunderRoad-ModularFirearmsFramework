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
            
            //item.OnHeldActionEvent += this.OnHeldAction;

            parentFirearm = this.GetComponent<Weapons.BaseFirearmGenerator>();

            if (!String.IsNullOrEmpty(module.ammoCounterRef))
            {
                //Debug.Log("[Fisher-ModularFirearms] Getting Ammo Counter Objects ...");
                ammoCounterMesh = item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>();
                digitsGridTexture = (Texture2D)item.GetCustomReference(module.ammoCounterRef).GetComponent<MeshRenderer>().material.mainTexture;
                //Debug.Log("[Fisher-ModularFirearms] GOT Ammo Counter Objects !!!");
            }

            //if (digitsGridTexture == null) Debug.LogError("[Fisher-ModularFirearms] COULD NOT GET GRID TEXTURE");
            //if (ammoCounterMesh == null) Debug.LogError("[Fisher-ModularFirearms] COULD NOT GET MESH RENDERER");

            if ((digitsGridTexture != null) && (ammoCounterMesh != null))
            {
                ammoCounter = new TextureProcessor();
                ammoCounter.SetGridTexture(digitsGridTexture);
                ammoCounter.SetTargetRenderer(ammoCounterMesh);
                //Debug.Log("[Fisher-ModularFirearms] Sucessfully Setup Ammo Counter!!");
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

        //protected void Start()
        //{

        //}


        //public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        //{

        //}
    }
}
