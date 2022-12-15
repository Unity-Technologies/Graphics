using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Previous Position Binder")]
    [VFXBinder("Transform/Position (Previous)")]
    class VFXPreviousPositionBinder : VFXSpaceableBinder
    {
        [VFXPropertyBinding("UnityEngine.Vector3")]
        public ExposedProperty m_Property = "PreviousPosition";
        public Transform Target = null;
        Vector3 oldPosition;

        protected override void OnEnable()
        {
            base.OnEnable();
            oldPosition = Target != null ? Target.position : Vector3.zero;
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(m_Property, oldPosition);
            var currentPosition = ApplySpacePosition(component, m_Property, Target.position);
            oldPosition = currentPosition;
        }

        public override string ToString()
        {
            return string.Format("Previous Position : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
