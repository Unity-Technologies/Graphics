#if VFX_OUTPUTEVENT_PHYSICS
namespace UnityEngine.VFX.Utility
{
    [RequireComponent(typeof(Rigidbody))]
    public class VFXOutputEventPrefabAttributeHandler_RigidBody : VFXOutputEventPrefabAttributeHandler
    {
        static readonly int kVelocity = Shader.PropertyToID("velocity");
        Rigidbody m_RigidBody;
        public enum Space
        {
            Local,
            World
        }
        public Space attributeSpace = Space.Local;

        public override void OnVFXEventAttribute(VFXEventAttribute eventAttribute, VisualEffect visualEffect)
        {
            var velocity = eventAttribute.GetVector3(kVelocity);

            if (attributeSpace == Space.Local)
                velocity = visualEffect.transform.localToWorldMatrix.MultiplyVector(velocity);
            
            m_RigidBody = GetComponent<Rigidbody>();
            m_RigidBody.WakeUp();
            m_RigidBody.velocity = velocity;
        }
    }
}
#endif
