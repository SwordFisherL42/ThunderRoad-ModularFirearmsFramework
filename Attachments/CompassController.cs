using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class CompassController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;
        private Light flashlightSource;
        private Handle attachmentHandle;
        private MeshRenderer ignoredMesh;
        private int lightCullingMask;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
            if (module.attachmentRef != null) flashlightSource = item.GetCustomReference(module.attachmentRef).GetComponent<Light>();
        }

        protected void Start()
        {

        }


        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {

        }

    }
}
