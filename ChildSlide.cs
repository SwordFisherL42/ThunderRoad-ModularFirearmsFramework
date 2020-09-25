using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    public class ChildSlide 
    {
        public Rigidbody rb;
        public bool initialCheck = false;
        protected Item parentItem;
        protected ItemModuleFirearmBase parentModule;
        protected float slideForwardForce;
        protected float slideBlowbackForce;

        protected float lockedAnchorOffset;
        protected float lockedBackAnchorOffset;

        protected ConfigurableJoint connectedJoint;

        public ChildSlide(Item Parent, ItemModuleFirearmBase ParentModule)
        {
            parentItem = Parent;
            parentModule = ParentModule;
            slideForwardForce = parentModule.slideForwardForce;
            slideBlowbackForce = parentModule.slideBlowbackForce;
            lockedAnchorOffset = parentModule.slideNeutralLockOffset;
            lockedBackAnchorOffset = parentModule.slideOffsetZ * 2.0f; //parentModule.slideRearLockOffset;
        }

        protected GameObject chamberBullet;
        protected Handle slideHandle;
        // Objects to generate
        protected ConstantForce slideForce;

        // State Machine Parameters
        protected Vector3 originalAnchor;
        protected bool isHeld;
        protected float directionModifer = 1.0f;
        protected bool isLockedBack;
        
        // BS/Unity Core Functions //
        public void InitializeSlide(GameObject slideObject)
        {
            slideHandle = slideObject.GetComponent<Handle>();
            connectedJoint = parentItem.gameObject.GetComponent<ConfigurableJoint>();
            rb = slideObject.AddComponent<Rigidbody>();
            slideForce = slideObject.AddComponent<ConstantForce>();
            connectedJoint.connectedBody = rb;

            if (!String.IsNullOrEmpty(parentModule.chamberBulletRef)) chamberBullet = parentItem.definition.GetCustomReference(parentModule.chamberBulletRef).gameObject;
            Debug.Log("[Fisher-Firearms] Child Slide Initialized !!!");
            //item = this.GetComponent<Item>();
            //item.OnHeldActionEvent += OnHeldAction;
            //item.OnGrabEvent += OnGrabEvent;
            //item.OnUngrabEvent += OnUngrabEvent;
            //module = item.data.GetModule<ItemModuleSlide>();
            //slideJoint = item.definition.GetCustomReference(module.slideJointRef).GetComponent<ConfigurableJoint>();
            //slideForce = item.transform.GetComponent<ConstantForce>();
            //slideHandle = item.definition.GetCustomReference(module.slideHandleRef).GetComponent<Handle>();
            //chamberShell = item.definition.GetCustomReference(module.chamberShellRef).GetComponent<MeshRenderer>();
            //chamberBullet = item.definition.GetCustomReference(module.chamberBulletRef).GetComponent<MeshRenderer>();

        }

        public void SetupSlide()
        {
            Debug.Log("[Fisher-Firearms] Slide Locked on start!");
            originalAnchor = connectedJoint.connectedAnchor;
            //Debug.Log("[Fisher-Firearms] originalAnchor" + originalAnchor);
            ChamberRoundVisible();
            LockSlide();
            Debug.Log("[Fisher-Firearms] Child Slide Setup !!!");
            return;
        }


        // State Functions //
        public void LockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, 0));
            connectedJoint.zMotion = ConfigurableJointMotion.Locked;
            if (isLockedBack)
            {
                connectedJoint.connectedAnchor = new Vector3(originalAnchor.x, originalAnchor.y, lockedBackAnchorOffset);
            }
            else
            {
                connectedJoint.connectedAnchor = new Vector3(originalAnchor.x, originalAnchor.y, lockedAnchorOffset);
            }
            //Debug.Log("[Fisher-Firearms] Locked anchor" + originalAnchor);
            DisableTouch();

        }

        public void UnlockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            connectedJoint.connectedAnchor = originalAnchor;
            EnableTouch();
        }

        public void ForwardState()
        {
            isLockedBack = false;
            directionModifer = 1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            connectedJoint.connectedAnchor = originalAnchor;
        }

        public void LockedBackState()
        {
            isLockedBack = true;
            directionModifer = -1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            connectedJoint.connectedAnchor = originalAnchor;
        }

        public void LastShot()
        {
            BlowBack(true);
            LockedBackState();
            return;
        }

        // Interaction Helper Functions //
        public void SetHeld(bool status) { isHeld = status; }

        public bool IsHeld()
        {
            return isHeld;
        }

        public bool IsLocked()
        {
            return isLockedBack;
        }

        // Base Functions //
        //public void DestoryThisSlide()
        //{

        //    //item.Despawn();
        //}

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
            chamberBullet.SetActive(isVisible);
            return;
        }

        protected void SetRelativeSlideForce(Vector3 newSlideForce)
        {
            slideForce.relativeForce = newSlideForce;
            return;
        }

        public void BlowBack(bool lastShot = false)
        {
            SetRelativeSlideForce(new Vector3(0, 0, slideForwardForce * 0.1f)); //Set forward spring to 10%
            rb.AddRelativeForce(Vector3.forward * -1.0f * slideBlowbackForce, ForceMode.Impulse); // Apply reverse force momentarily 
            if (!lastShot) SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce)); // Restore previous forward spring
            return;
        }

        protected GameObject GetParentObj()
        {
            return connectedJoint.gameObject;
        }

    }
}


//protected void OnHeldAction(Interactor interactor, Handle handle, Interactable.Action action)
//{
//    if (action == Interactable.Action.Ungrab)
//    {
//        //Debug.Log("[Fisher-Firearms] Slide Ungrab!");
//        isHeld = false;
//    }
//}

//public void IgnoreCollisionWithProjectile(Item Projectile)
//{
//    item.IgnoreObjectCollision(Projectile);
//    return;
//}

//public void OnUngrabEvent(Handle handle, Interactor interactor, bool throwing)
//{
//    //Debug.Log("[Fisher-Firearms] Slide UnGrab!");
//    isHeld = false;
//}

//public void OnGrabEvent(Handle handle, Interactor interactor)
//{
//    //Debug.Log("[Fisher-Firearms] Slide Grab!");
//    isHeld = true;
//    if (isLockedBack)
//    {
//        ForwardState();
//    }
//}

//protected void Update()
//{
//    if (initialCheck) return;
//    try
//    {
//        if (GetParentObj().GetComponent<ItemFirearmBase>().gunGripHeldRight || GetParentObj().GetComponent<ItemFirearmBase>().gunGripHeldLeft)
//        {
//            UnlockSlide();
//            initialCheck = true;
//        }
//    }
//    catch { Debug.Log("[Fisher-Firearms] Slide EXCEPTION"); }

//}
