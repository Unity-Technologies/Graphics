#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/UI Toggle Binder")]
    [VFXBinder("UI/Toggle")]
    class VFXUIToggleBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "BoolParameter";
        public Toggle Target = null;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasBool(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetBool(m_Property, Target.isOn);
        }

        public override string ToString()
        {
            return string.Format("UI Toggle : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
