using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Enabled Binder")]
    [VFXBinder("GameObject/Enabled")]
    class VFXEnabledBinder : VFXBinderBase
    {
        public enum Check
        {
            ActiveInHierarchy = 0,
            ActiveSelf = 1
        }

        public string Property { get { return (string)m_Property; } set { m_Property = value; } }
        public Check check = Check.ActiveInHierarchy;
        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
        protected ExposedProperty m_Property = "Enabled";
        public GameObject Target = null;

        public override bool IsValid(VisualEffect component)
        {
            return Target != null && component.HasBool(m_Property);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetBool(m_Property, check == 0 ? Target.activeInHierarchy : Target.activeSelf );
        }

        public override string ToString()
        {
            return string.Format("{2} : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name, check);
        }
    }
}
