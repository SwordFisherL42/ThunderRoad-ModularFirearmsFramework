using System;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    public class ChildRigidbodyController
    {
        public GameObject thisSlideObject;
        public Rigidbody rb;
        public bool initialCheck = false;

        protected Item parentItem;
        protected Shared.FirearmModule parentModule;
        
        private readonly float slideForwardForce;
        private readonly float slideBlowbackForce;

        private readonly float lockedAnchorOffset;
        private readonly float lockedBackAnchorOffset;

        public ChildRigidbodyController(Item Parent, Shared.FirearmModule ParentModule)
        {
            parentItem = Parent;
            parentModule = ParentModule;
            slideForwardForce = parentModule.slideForwardForce;
            slideBlowbackForce = parentModule.slideBlowbackForce;
            lockedAnchorOffset = parentModule.slideNeutralLockOffset;
            lockedBackAnchorOffset = -1.0f * parentModule.slideTravelDistance;
        }

        private Handle slideHandle;
        private ConstantForce slideForce;
        private ConfigurableJoint connectedJoint;
        private GameObject chamberBullet;

        // State Machine Parameters
        private Vector3 currentAnchor;
        private Vector3 originalAnchor;
        private Vector3 lockedBackAnchor;
        private Vector3 lockedNeutralAnchor;

        private bool isHeld;
        private float directionModifer = 1.0f;
        private bool isLockedBack;


        // BS/Unity Core Functions //
        public void InitializeSlide(GameObject slideObject)
        {
            try
            {
                rb = slideObject.GetComponent<Rigidbody>();
                slideHandle = slideObject.GetComponent<Handle>();
                slideForce = slideObject.GetComponent<ConstantForce>();
                connectedJoint = parentItem.gameObject.GetComponent<ConfigurableJoint>();
                thisSlideObject = slideObject;
                if (!String.IsNullOrEmpty(parentModule.chamberBulletRef)) chamberBullet = parentItem.GetCustomReference(parentModule.chamberBulletRef).gameObject;
            }
            catch { Debug.LogError("[ModularFirearmsFramework][EXCEPTION] Unable to Initialize CRC ! "); }
        }

        public void SetupSlide()
        {
            originalAnchor = new Vector3(0, 0, -0.5f * parentModule.slideTravelDistance);
            lockedBackAnchor = new Vector3(0, 0, lockedBackAnchorOffset);
            lockedNeutralAnchor = new Vector3(0, 0, lockedAnchorOffset);
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
            ChamberRoundVisible(false);
            LockSlide();
        }

            // State Functions //

            public void SetLockedState(bool forward = true)
        {
            SetRelativeSlideForce(new Vector3(0, 0, 0));
            connectedJoint.zMotion = ConfigurableJointMotion.Locked;
            if (forward)
            {
                currentAnchor = lockedNeutralAnchor;
                connectedJoint.anchor = currentAnchor;
            }
            else
            {
                currentAnchor = lockedBackAnchor;
                connectedJoint.anchor = currentAnchor;
            }
        }


        /// <summary>
        /// Used for locking the child body when the weapon is released.
        /// Stops forces on the Rigidbody, locks the Configurable Joint and sets the anchor position based on current state,
        /// and finally disables touch interaction.
        /// </summary>
        public void LockSlide(bool disable_touch = true)
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
            if (disable_touch) DisableTouch();
        }

        /// <summary>
        /// Used for enabling the dynamic slide mechanics when the weapon is grabbed.
        /// Re-Enables touch, Resets the Configurable joint and the relative forward-force based on machine state.
        /// </summary>
        public void UnlockSlide(bool enable_touch = true)
        {
            if (enable_touch) EnableTouch();
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
        }

        /// <summary>
        /// Sets the Child Object to its default position, with Configurable Joint limits for manipulation
        /// Sets the State parameters, joint settings and forces for `ForwardState`
        /// </summary>
        public void ForwardState()
        {
            isLockedBack = false;
            directionModifer = 1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
        }

        /// <summary>
        /// Sets the Child Object to its default position, with Configurable Joint limits for manipulation
        /// Sets the State parameters, joint settings and forces for `ForwardState`
        /// </summary>
        public void LockedBackState()
        {
            isLockedBack = true;
            directionModifer = -1.0f;
            SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce));
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            currentAnchor = originalAnchor;
            connectedJoint.anchor = currentAnchor;
        }

        /// <summary>
        /// Apply an Impulse force in the reverse direction.
        /// Decrease the forward force, add a relative force in the -ve Z direction,
        /// and optionally leave the forward spring disabled if this is the last shot
        /// </summary>
        /// <param name="lastShot"></param>
        public void BlowBack(bool lastShot = false)
        {
            SetRelativeSlideForce(new Vector3(0, 0, slideForwardForce * 0.1f)); //Set forward spring to 10%
            rb.AddRelativeForce(Vector3.forward * -1.0f * slideBlowbackForce, ForceMode.Impulse); // Apply reverse force momentarily 
            if (!lastShot) SetRelativeSlideForce(new Vector3(0, 0, directionModifer * slideForwardForce)); // Restore previous forward spring
            ChamberRoundVisible(false);
            return;
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

        public void SetRelativeSlideForce(Vector3 newSlideForce)
        {
            slideForce.relativeForce = newSlideForce;
            return;
        }

        // Debugging Functions ...
        // Set defaults when Unity engine resets our values aribtrarily..
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

        public GameObject GetConnectedObj()
        {
            return connectedJoint.gameObject;
        }

        public void DestroyJoint()
        {
            GameObject.Destroy(slideForce);
            GameObject.Destroy(connectedJoint);
            thisSlideObject.transform.parent = null;
        }
    }
}
