using UnityEngine;
using ThunderRoad;

namespace ModularFirearms.Weapons
{
    public class GrenadeGenerator : MonoBehaviour
    {
        protected Item item;
        protected Shared.FirearmModule module;
        public ConfigurableJoint connectedJoint;
        /// Slide Interaction ///
        protected Handle slideHandle;
        //private ChildRigidbodyController slideController;
        //private GameObject slideObject;
        //private GameObject slideCenterPosition;
        //private ConstantForce slideForce;
        //private Rigidbody slideRB;
        //private SphereCollider slideCapsuleStabilizer;


        //void Awake() { }

        //void Start()
        //{
        //    InitializeConfigurableJoint(module.slideStabilizerRadius);
        //}

        // private void LateUpdate() { }

    }
}
