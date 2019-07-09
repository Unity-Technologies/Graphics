#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/UI Slider Parameter Binder")]
    [VFXBinder("UI/Slider")]
    class VFXUISliderBinder : VFXBinderBase
    {
        public string Property { get { return (string)m_Property; } set { m_Property = value; } }

        [VFXPropertyBinding("System.Single"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "FloatParameter";
        public Slider Target = null;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasFloat(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetFloat(m_Property, Target.value);
        }

        public override string ToString()
        {
            return string.Format("UI Slider : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
