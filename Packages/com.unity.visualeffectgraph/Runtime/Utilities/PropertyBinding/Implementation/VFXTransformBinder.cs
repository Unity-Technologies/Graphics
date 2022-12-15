using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Transform Binder")]
    [VFXBinder("Transform/Transform")]
    class VFXTransformBinder : VFXSpaceableBinder
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; UpdateSubProperties(); } }

        [VFXPropertyBinding("UnityEditor.VFX.Transform"), SerializeField]
        protected ExposedProperty m_Property = "Transform";
        public Transform Target = null;

        private ExposedProperty Position;
        private ExposedProperty Angles;
        private ExposedProperty Scale;
        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateSubProperties();
        }

        void OnValidate()
        {
            UpdateSubProperties();
        }

        void UpdateSubProperties()
        {
            Position = m_Property + "_position";
            Angles = m_Property + "_angles";
            Scale = m_Property + "_scale";
        }

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasVector3((int)Position) && component.HasVector3((int)Angles) && component.HasVector3((int)Scale);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            ApplySpaceTRS(component, Position, Target, out var position, out var eulerAngles, out var scale);
            component.SetVector3((int)Position, position);
            component.SetVector3((int)Angles, eulerAngles);
            component.SetVector3((int)Scale, scale);
        }

        public override string ToString()
        {
            return string.Format("Transform : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
