using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    public class ItemSlide : MonoBehaviour
    {
        protected Item item;
        protected ItemModuleSlide module;
        protected ConfigurableJoint slideJoint;
        protected ConstantForce slideForce;
        protected Handle slideHandle;
        protected MeshRenderer chamberShell;
        protected MeshRenderer chamberBullet;
        protected Vector3 originalAnchor;
        protected bool isHeld;
        protected float directionModifer = 1.0f;
        protected bool isLockedBack;
        protected bool initialCheck;
        protected float lockedAnchorOffset = 0.0f;
        protected float lockedBackAnchorOffset = 0.05f;

        // BS/Unity Core Functions //
        protected void Awake()
        {
            item = this.GetComponent<Item>();
            item.OnHeldActionEvent += OnHeldAction;
            item.OnGrabEvent += OnGrabEvent;
            item.OnUngrabEvent += OnUngrabEvent;
            module = item.data.GetModule<ItemModuleSlide>();
            slideJoint = item.definition.GetCustomReference(module.slideJointRef).GetComponent<ConfigurableJoint>();
            slideForce = item.transform.GetComponent<ConstantForce>();
            slideHandle = item.definition.GetCustomReference(module.slideHandleRef).GetComponent<Handle>();
            chamberShell = item.definition.GetCustomReference(module.chamberShellRef).GetComponent<MeshRenderer>();
            chamberBullet = item.definition.GetCustomReference(module.chamberBulletRef).GetComponent<MeshRenderer>();
        }

        protected void Start()
        {
            //Debug.Log("[Fisher-Firearms] Slide Locked on start!");
            originalAnchor = slideJoint.connectedAnchor;
            //Debug.Log("[Fisher-Firearms] originalAnchor" + originalAnchor);
            ChamberRoundVisible();
            LockSlide();
            return;
        }

        protected void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.Ungrab)
            {
                //Debug.Log("[Fisher-Firearms] Slide Ungrab!");
                isHeld = false;
            }
        }

        public void IgnoreCollisionWithProjectile(Item Projectile)
        {
            item.IgnoreObjectCollision(Projectile);
            return;
        }

        public void OnUngrabEvent(Handle handle, Interactor interactor, bool throwing)
        {
            //Debug.Log("[Fisher-Firearms] Slide UnGrab!");
            isHeld = false;
        }

        public void OnGrabEvent(Handle handle, Interactor interactor)
        {
            //Debug.Log("[Fisher-Firearms] Slide Grab!");
            isHeld = true;
            if (isLockedBack)
            {
                ForwardState();
            }
        }

        protected void Update()
        {
            if (initialCheck) return;
            try
            {
                if (GetParentObj().GetComponent<ItemFirearmBase>().gunGripHeldRight || GetParentObj().GetComponent<ItemFirearmBase>().gunGripHeldLeft)
                {
                    UnlockSlide();
                    initialCheck = true;
                }
            }
            catch { Debug.Log("[Fisher-Firearms] Slide EXCEPTION"); }

        }

        // State Functions //
        public void LockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, 0));
            slideJoint.zMotion = ConfigurableJointMotion.Locked;
            if (isLockedBack)
            {
                slideJoint.connectedAnchor = new Vector3(originalAnchor.x, originalAnchor.y, lockedBackAnchorOffset);
            }
            else
            {
                slideJoint.connectedAnchor = new Vector3(originalAnchor.x, originalAnchor.y, lockedAnchorOffset);
            }
            //Debug.Log("[Fisher-Firearms] Locked anchor" + originalAnchor);
            DisableTouch();

        }

        public void UnlockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * module.slideForwardForce));
            slideJoint.zMotion = ConfigurableJointMotion.Limited;
            slideJoint.connectedAnchor = originalAnchor;
            EnableTouch();
        }

        public void ForwardState()
        {
            isLockedBack = false;
            directionModifer = 1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * module.slideForwardForce));
            slideJoint.zMotion = ConfigurableJointMotion.Limited;
            slideJoint.connectedAnchor = originalAnchor;
        }

        public void LockedBackState()
        {
            isLockedBack = true;
            directionModifer = -1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * module.slideForwardForce));
            slideJoint.zMotion = ConfigurableJointMotion.Limited;
            slideJoint.connectedAnchor = originalAnchor;
        }

        public void LastShot()
        {
            BlowBack(true);
            LockedBackState();
            return;
        }

        // Interaction Helper Functions //
        public bool IsHeld()
        {
            return isHeld;
        }

        public bool IsLocked()
        {
            return isLockedBack;
        }

        // Base Functions //
        public void DestoryThisSlide()
        {
            item.Despawn();
        }

        public void DisableTouch()
        {
            slideHandle.SetTouch(false);
        }

        public void EnableTouch()
        {
            slideHandle.SetTouch(true);
        }

        public void ChamberRoundVisible(bool isVisible = false)
        {
            chamberShell.enabled = isVisible;
            chamberBullet.enabled = isVisible;
            return;
        }

        public void ChamberBulletVisible(bool isVisible = false)
        {
            chamberBullet.enabled = isVisible;
            return;
        }

        public void ChamberShellVisible(bool isVisible = false)
        {
            chamberShell.enabled = isVisible;
            return;
        }

        protected void SetRelativeSlideForce(Vector3 newSlideForce)
        {
            slideForce.relativeForce = newSlideForce;
            return;
        }

        public void BlowBack(bool lastShot = false)
        {
            SetRelativeSlideForce(new Vector3(0, 0, module.slideForwardForce*0.1f)); //Set forward spring to 10%
            item.rb.AddRelativeForce(Vector3.forward * -1.0f * module.slideBlowbackForce, ForceMode.Impulse); // Apply reverse force momentarily 
            if (!lastShot) SetRelativeSlideForce(new Vector3(0, 0, directionModifer * module.slideForwardForce)); // Restore previous forward spring
            return;
        }

        protected GameObject GetParentObj()
        {
            return slideJoint.gameObject;
        }

    }
}
