using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Enabled Binder")]
    [VFXBinder("GameObject/Enabled")]
    public class VFXEnabledBinder : VFXBinderBase
    {
        public enum Check
        {
            ActiveInHierarchy = 0,
            ActiveSelf = 1
        }

        public string Parameter { get { return (string)m_Parameter; } set { m_Parameter = value; } }
        public Check check = Check.ActiveInHierarchy;
        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_Parameter = "Enabled";
        public GameObject Target;



        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasBool(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetBool(m_Parameter, check == 0 ? Target.activeInHierarchy : Target.activeSelf );
        }

        public override string ToString()
        {
            return string.Format("{2} : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name, check);
        }
    }
}
