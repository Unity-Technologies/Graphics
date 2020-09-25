#if VFX_OUTPUTEVENT_CINEMACHINE_2_6_0_OR_NEWER
using Cinemachine;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    class VFXOutputEventCinemachineCameraShake : VFXOutputEventAbstractHandler
    {
        public override bool canExecuteInEditor => true;

        public enum Space
        {
            Local,
            World
        }

        static readonly int k_Position = Shader.PropertyToID("position");
        static readonly int k_Velocity = Shader.PropertyToID("velocity");

        [Tooltip("The Cinemachine Impulse Source to use in order to send impulses.")]
        public CinemachineImpulseSource cinemachineImpulseSource;
        [Tooltip("The space in which the position and velocity attributes values are defined (local to the VFX, or world).")]
        public Space attributeSpace;

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if (cinemachineImpulseSource != null)
            {
                Vector3 pos = eventAttribute.GetVector3(k_Position);
                Vector3 vel = eventAttribute.GetVector3(k_Velocity);

                if (attributeSpace == Space.Local)
                {
                    pos = transform.localToWorldMatrix.MultiplyPoint(pos);
                    vel = transform.localToWorldMatrix.MultiplyVector(vel);
                }

                cinemachineImpulseSource.GenerateImpulseAt(pos, vel);
            }
        }
    }
}
#endif
