using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Velocity Binder")]
    [VFXBinder("Transform/Velocity")]
    class VFXVelocityBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        public ExposedProperty m_Property = "Velocity";
        public Transform Target = null;

        private static readonly float invalidPreviousTime = -1.0f;
        private float m_PreviousTime = invalidPreviousTime;
        private Vector3 m_PreviousPosition = Vector3.zero;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)m_Property);
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

            component.SetVector3((int)m_Property, velocity);
            m_PreviousPosition = Target.transform.position;
            m_PreviousTime = time;
        }

        public override string ToString()
        {
            return string.Format("Velocity : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
