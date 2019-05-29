using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Velocity Binder")]
    [VFXBinder("Transform/Velocity")]
    public class VFXVelocityBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("UnityEngine.Vector3"), SerializeField]
        public ExposedParameter m_Parameter = "Velocity";
        public Transform Target;

        private static readonly float invalidPreviousTime = -1.0f;
        private float m_PreviousTime = invalidPreviousTime;
        private Vector3 m_PreviousPosition = Vector3.zero;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)m_Parameter);
        }

        public override void Reset()
        {
            m_PreviousTime = invalidPreviousTime;
        }

        public override void UpdateBinding(VisualEffect component)
        {
            Vector3 velocity = Vector3.zero;
            float time;
#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
                time = (float)UnityEditor.EditorApplication.timeSinceStartup;
            else
#endif
            time = Time.time;

            if (m_PreviousTime != invalidPreviousTime)
            {
                var delta = Target.transform.position - m_PreviousPosition;
                var deltaTime = time - m_PreviousTime;
                if (Vector3.SqrMagnitude(delta) > Mathf.Epsilon && deltaTime > Mathf.Epsilon)
                    velocity = delta / deltaTime;
            }

            component.SetVector3((int)m_Parameter, velocity);
            m_PreviousPosition = Target.transform.position;
            m_PreviousTime = time;
        }

        public override string ToString()
        {
            return string.Format("Velocity : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
