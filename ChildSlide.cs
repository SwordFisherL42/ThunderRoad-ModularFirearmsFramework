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
            lockedBackAnchorOffset = -2.0f * parentModule.slideTravelDistance; //parentModule.slideRearLockOffset;
        }

        private GameObject chamberBullet;
        private Handle slideHandle;
        private ConstantForce slideForce;

        // State Machine Parameters
        private Vector3 originalAnchor;
        private Vector3 lockedBackAnchor;
        private Vector3 lockedNeutralAnchor;

        private Vector3 currentAnchor;

        private bool isHeld;
        private float directionModifer = 1.0f;
        private bool isLockedBack;
        
        // BS/Unity Core Functions //
        public void InitializeSlide(GameObject slideObject)
        {
            slideHandle = slideObject.GetComponent<Handle>();
            slideForce = slideObject.GetComponent<ConstantForce>();
            rb = slideObject.GetComponent<Rigidbody>();
            connectedJoint = parentItem.gameObject.GetComponent<ConfigurableJoint>();
            if (!String.IsNullOrEmpty(parentModule.chamberBulletRef)) chamberBullet = parentItem.definition.GetCustomReference(parentModule.chamberBulletRef).gameObject;
            Debug.Log("[Fisher-Firearms] Child Slide Initialized !!!");
            DumpJoint();
            //rb.mass = 1.0f;
            //rb.drag = 0.0f;
            //rb.angularDrag = 0.05f;
            //rb.useGravity = true;
            //rb.isKinematic = false;
            //rb.interpolation = RigidbodyInterpolation.None;
            //rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            //connectedJoint.connectedBody = rb;
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
            originalAnchor = new Vector3(0, 0, -1.0f * parentModule.slideTravelDistance);
            lockedBackAnchor = new Vector3(0, 0, lockedBackAnchorOffset);
            lockedNeutralAnchor = new Vector3(0, 0, lockedAnchorOffset);
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            //Debug.Log("[Fisher-Firearms] originalAnchor" + originalAnchor);
            ChamberRoundVisible();
            LockSlide();
            Debug.Log("[Fisher-Firearms] Child Slide Setup !!!");
            DumpJoint();
            return;
        }


        // State Functions //
        public void LockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, 0));
            connectedJoint.zMotion = ConfigurableJointMotion.Locked;
            if (isLockedBack)
            {
                currentAnchor = lockedBackAnchor;
                connectedJoint.anchor = currentAnchor;
            }
            else
            {
                currentAnchor = lockedNeutralAnchor;
                connectedJoint.anchor = currentAnchor;
            }
            //Debug.Log("[Fisher-Firearms] Locked anchor" + originalAnchor);
            DisableTouch();
            DumpJoint();
        }

        public void ForceJointConfig()
        {
            if (connectedJoint.anchor.z != currentAnchor.z)
            {
                connectedJoint.anchor = new Vector3(0, 0, -1.0f * parentModule.slideTravelDistance);
            }
        }

        public void DumpJoint()
        {
            Debug.Log("connectedJoint.connectedBody " + connectedJoint.connectedBody.ToString());
            Debug.Log("connectedJoint.anchor " + connectedJoint.anchor.ToString());
            Debug.Log("connectedJoint.connectedAnchor " + connectedJoint.connectedAnchor.ToString());
            Debug.Log("connectedJoint.linearLimit.limit " + connectedJoint.linearLimit.limit.ToString());
            Debug.Log("connectedJoint.autoConfigureConnectedAnchor " + connectedJoint.autoConfigureConnectedAnchor.ToString());
        }
        public void UnlockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            DumpJoint();
            EnableTouch();
        }

        public void ForwardState()
        {
            isLockedBack = false;
            directionModifer = 1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            DumpJoint();
        }

        public void LockedBackState()
        {
            isLockedBack = true;
            directionModifer = -1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            DumpJoint();
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
