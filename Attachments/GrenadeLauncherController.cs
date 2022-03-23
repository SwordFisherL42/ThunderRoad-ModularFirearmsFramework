using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Attachments
{
    public class GrenadeLauncherController : MonoBehaviour
    {
        protected Item item;
        protected Shared.AttachmentModule module;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<Shared.AttachmentModule>();
            item.OnHeldActionEvent += this.OnHeldAction;
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {

        }

    }
}
