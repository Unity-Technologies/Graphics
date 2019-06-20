#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX UI Slider Parameter Binder")]
    [VFXBinder("UI/Slider")]
    public class VFXUISliderBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_Parameter = "FloatParameter";
        public Slider Target;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasFloat(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetFloat(m_Parameter, Target.value);
        }

        public override string ToString()
        {
            return string.Format("UI Slider : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
