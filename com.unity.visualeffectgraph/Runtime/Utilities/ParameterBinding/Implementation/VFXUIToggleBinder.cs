#if VFX_HAS_UI
using UnityEngine.UI;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX UI Toggle Parameter Binder")]
    [VFXBinder("UI/Toggle")]
    public class VFXUIToggleBinder : VFXBinderBase
    {
        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_Parameter = "BoolParameter";
        public Toggle Target;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasBool(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetBool(m_Parameter, Target.isOn);
        }

        public override string ToString()
        {
            return string.Format("UI Toggle : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}
#endif
