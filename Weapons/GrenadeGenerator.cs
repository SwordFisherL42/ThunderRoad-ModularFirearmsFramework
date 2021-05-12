using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;
using static ModularFirearms.FirearmFunctions;

namespace ModularFirearms.Weapons
{
    public class GrenadeGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;
        public ConfigurableJoint connectedJoint;
        /// Slide Interaction ///
        protected Handle slideHandle;
        private Shared.ChildRigidbodyController slideController;
        private GameObject slideObject;
        private GameObject slideCenterPosition;
        private ConstantForce slideForce;
        private Rigidbody slideRB;
        private SphereCollider slideCapsuleStabilizer;


        void Awake() { }

        void Start()
        {
            InitializeConfigurableJoint(module.slideStabilizerRadius);
        }

        private void LateUpdate() { }

        private void InitializeConfigurableJoint(float stabilizerRadius)
        {
            slideRB = slideObject.GetComponent<Rigidbody>();
            if (slideRB == null)
            {
                // TODO: Figure out why adding RB from code doesnt work
                slideRB = slideObject.AddComponent<Rigidbody>();
                //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] CREATED Rigidbody ON SlideObject...");

            }
            //else { Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] ACCESSED Rigidbody on Slide Object..."); }

            slideRB.mass = 1.0f;
            slideRB.drag = 0.0f;
            slideRB.angularDrag = 0.05f;
            slideRB.useGravity = true;
            slideRB.isKinematic = false;
            slideRB.interpolation = RigidbodyInterpolation.None;
            slideRB.collisionDetectionMode = CollisionDetectionMode.Discrete;

            slideCapsuleStabilizer = slideCenterPosition.AddComponent<SphereCollider>();
            slideCapsuleStabilizer.radius = stabilizerRadius;
            slideCapsuleStabilizer.gameObject.layer = 21;
            Physics.IgnoreLayerCollision(21, 12);
            Physics.IgnoreLayerCollision(21, 15);
            Physics.IgnoreLayerCollision(21, 22);
            Physics.IgnoreLayerCollision(21, 23);
            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created Stabilizing Collider on Slide Object");

            slideForce = slideObject.AddComponent<ConstantForce>();
            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created ConstantForce on Slide Object");

            //  Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Creating Config Joint and Setting Joint Values...");
            connectedJoint = item.gameObject.AddComponent<ConfigurableJoint>();
            connectedJoint.connectedBody = slideRB;
            connectedJoint.anchor = new Vector3(0, 0, -0.5f * module.slideTravelDistance);
            connectedJoint.axis = Vector3.right;
            connectedJoint.autoConfigureConnectedAnchor = false;
            connectedJoint.connectedAnchor = Vector3.zero;//new Vector3(0.04f, -0.1f, -0.22f);
            connectedJoint.secondaryAxis = Vector3.up;
            connectedJoint.xMotion = ConfigurableJointMotion.Locked;
            connectedJoint.yMotion = ConfigurableJointMotion.Locked;
            connectedJoint.zMotion = ConfigurableJointMotion.Limited;
            connectedJoint.angularXMotion = ConfigurableJointMotion.Locked;
            connectedJoint.angularYMotion = ConfigurableJointMotion.Locked;
            connectedJoint.angularZMotion = ConfigurableJointMotion.Locked;
            connectedJoint.linearLimit = new SoftJointLimit { limit = 0.5f * module.slideTravelDistance, bounciness = 0.0f, contactDistance = 0.0f };
            connectedJoint.massScale = 1.0f;
            connectedJoint.connectedMassScale = module.slideMassOffset;
            // Debug.Log("[Fisher-ModularFirearms][Config-Joint-Init] Created Configurable Joint !");
            //DumpRigidbodyToLog(slideRB);
        }

    }
}
