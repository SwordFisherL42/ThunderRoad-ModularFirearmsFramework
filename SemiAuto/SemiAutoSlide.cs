using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.SemiAuto
{
    public class SemiAutoSlide 
    {
        public Rigidbody rb;
        public bool initialCheck = false;

        protected Item parentItem;
        protected SemiAutoModule parentModule;
        protected float slideForwardForce;
        protected float slideBlowbackForce;

        protected float lockedAnchorOffset;
        protected float lockedBackAnchorOffset;

        protected ConfigurableJoint connectedJoint;

        public SemiAutoSlide(Item Parent, SemiAutoModule ParentModule)
        {
            parentItem = Parent;
            parentModule = ParentModule;
            slideForwardForce = parentModule.slideForwardForce;
            slideBlowbackForce = parentModule.slideBlowbackForce;
            lockedAnchorOffset = parentModule.slideNeutralLockOffset;
            lockedBackAnchorOffset = -1.0f * parentModule.slideTravelDistance;
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
            rb = slideObject.GetComponent<Rigidbody>();
            slideHandle = slideObject.GetComponent<Handle>();
            slideForce = slideObject.GetComponent<ConstantForce>();
            connectedJoint = parentItem.gameObject.GetComponent<ConfigurableJoint>();
            if (!String.IsNullOrEmpty(parentModule.chamberBulletRef)) chamberBullet = parentItem.definition.GetCustomReference(parentModule.chamberBulletRef).gameObject;
            Debug.Log("[Fisher-Firearms] Child Slide Initialized !!!");
            //DumpJoint();
        }

        public void SetupSlide()
        {
            Debug.Log("[Fisher-Firearms] Slide Locked on start!");
            originalAnchor = new Vector3(0, 0, -0.5f * parentModule.slideTravelDistance);
            lockedBackAnchor = new Vector3(0, 0, lockedBackAnchorOffset);
            lockedNeutralAnchor = new Vector3(0, 0, lockedAnchorOffset);
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            ChamberRoundVisible();
            LockSlide();
            Debug.Log("[Fisher-Firearms] Child Slide Setup !!!");
            //DumpJoint();
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
            DisableTouch();
            //DumpJoint();
        }

        // Set defaults when Unity engine is being a stupid little bastard.
        public void FixCustomComponents()
        {
            if (connectedJoint.anchor.z != currentAnchor.z)
            {
                connectedJoint.anchor = new Vector3(0, 0, currentAnchor.z);
            }

            //if (rb.isKinematic)
            //{
            //    rb.mass = 1.0f;
            //    rb.drag = 0.0f;
            //    rb.angularDrag = 0.05f;
            //    rb.useGravity = true;
            //    rb.isKinematic = false;
            //    rb.interpolation = RigidbodyInterpolation.None;
            //    rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            //}
        }

        public void DumpJoint()
        {
            Debug.Log("connectedJoint.connectedBody " + connectedJoint.connectedBody.ToString());
            Debug.Log("connectedJoint.anchor " + connectedJoint.anchor.ToString());
            Debug.Log("connectedJoint.connectedAnchor " + connectedJoint.connectedAnchor.ToString());
            Debug.Log("connectedJoint.linearLimit.limit " + connectedJoint.linearLimit.limit.ToString());
            Debug.Log("connectedJoint.autoConfigureConnectedAnchor " + connectedJoint.autoConfigureConnectedAnchor.ToString());
        }

        public void DumpRB()
        {
            Debug.Log("rb.mass " + rb.mass.ToString());
            Debug.Log("rb.drag " + rb.drag.ToString());
            Debug.Log("rb.angularDrag " + rb.angularDrag.ToString());
            Debug.Log("rb.isKinematic " + rb.isKinematic.ToString());
            Debug.Log("rb.useGravity " + rb.useGravity.ToString());
            Debug.Log("rb.detectCollisions " + rb.detectCollisions.ToString());
            Debug.Log("rb.collisionDetectionMode " + rb.collisionDetectionMode.ToString());
        }

        public void UnlockSlide()
        {
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            //DumpJoint();
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
            //DumpJoint();
        }

        public void LockedBackState()
        {
            isLockedBack = true;
            directionModifer = -1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            //DumpJoint();
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
            if (chamberBullet != null) { chamberBullet.SetActive(isVisible); }
            
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
