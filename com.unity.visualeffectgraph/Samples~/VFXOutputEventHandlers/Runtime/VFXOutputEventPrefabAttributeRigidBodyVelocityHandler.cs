#if VFX_OUTPUTEVENT_PHYSICS
namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(Rigidbody))]
    class VFXOutputEventPrefabAttributeRigidBodyVelocityHandler : VFXOutputEventPrefabAttributeAbstractHandler
    {
        Rigidbody m_RigidBody;

        public enum Space
        {
            Local,
            World
        }
        public Space attributeSpace;

        static readonly int k_Velocity = Shader.PropertyToID("velocity");
        public override void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect)
        {
            var velocity = eventAttribute.GetVector3(k_Velocity);
            if (attributeSpace == Space.Local)
                velocity = visualEffect.transform.localToWorldMatrix.MultiplyVector(velocity);

            if (TryGetComponent<Rigidbody>(out m_RigidBody))
            {
                m_RigidBody.WakeUp();
                m_RigidBody.velocity = velocity;
            }
        }
    }
}
#endif
