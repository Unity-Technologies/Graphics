#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX UI Dropdown Parameter Binder")]
    [VFXBinder("UI/Dropdown")]
    public class VFXUIDropdownBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("System.Int32"), SerializeField]
        protected ExposedParameter m_Parameter = "IntParameter";
        public Dropdown Target;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasInt(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetInt(m_Parameter, Target.value);
        }

        public override string ToString()
        {
            return string.Format("UI Dropdown : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
