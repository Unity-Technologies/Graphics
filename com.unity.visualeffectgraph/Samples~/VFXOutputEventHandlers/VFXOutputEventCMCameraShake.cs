#if CINEMACHINE
using Cinemachine;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public class VFXOutputEventCMCameraShake : VFXOutputEventHandler
    {
        public enum Space
        {
            Local,
            World
        }

        static readonly int position = Shader.PropertyToID("position");
        static readonly int velocity = Shader.PropertyToID("velocity");

        public CinemachineImpulseSource cinemachineImpulseSource;
        public Space AttributeSpace = Space.Local;

        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            if(cinemachineImpulseSource != null)
            {
                Vector3 pos = eventAttribute.GetVector3(position);
                Vector3 vel = eventAttribute.GetVector3(velocity);

                if(AttributeSpace == Space.Local)
                {
                    pos = transform.localToWorldMatrix.MultiplyPoint(pos);
                    vel = transform.localToWorldMatrix.MultiplyVector(vel);
                }

                cinemachineImpulseSource.GenerateImpulseAt( pos , vel );
            }
        }
    }
}
#endif