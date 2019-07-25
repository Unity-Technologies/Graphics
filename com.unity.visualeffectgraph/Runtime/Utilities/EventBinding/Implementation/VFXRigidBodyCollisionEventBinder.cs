using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class VFXRigidBodyCollisionEventBinder : VFXEventBinderBase
    {
        private ExposedProperty positionParameter = "position";
        private ExposedProperty directionParameter = "velocity";


        protected override void SetEventAttribute(object[] parameters)
        {
            ContactPoint contact = (ContactPoint)parameters[0];
            eventAttribute.SetVector3(positionParameter, contact.point);
            eventAttribute.SetVector3(directionParameter, contact.normal);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Debug-draw all contact points and normals
            foreach (ContactPoint contact in collision.contacts)
            {
                SendEventToVisualEffect(contact);
            }
        }
    }
}
