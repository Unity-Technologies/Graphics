#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/UI Dropdown Parameter Binder")]
    [VFXBinder("UI/Dropdown")]
    class VFXUIDropdownBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }

        [VFXPropertyBinding("System.Int32"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "IntParameter";
        public Dropdown Target = null;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasInt(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetInt(m_Property, Target.value);
        }

        public override string ToString()
        {
            return string.Format("UI Dropdown : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
