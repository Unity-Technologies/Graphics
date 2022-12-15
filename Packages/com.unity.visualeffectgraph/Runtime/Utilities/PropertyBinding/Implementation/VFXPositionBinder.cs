using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Position Binder")]
    [VFXBinder("Transform/Position")]
    class VFXPositionBinder : VFXSpaceableBinder
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }

        [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField]
        protected ExposedProperty m_Property = "Position";
        public Transform Target = null;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            var position = ApplySpacePosition(component, m_Property, Target.transform.position);
            component.SetVector3(m_Property, position);
        }

        public override string ToString()
        {
            return string.Format("Position : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
