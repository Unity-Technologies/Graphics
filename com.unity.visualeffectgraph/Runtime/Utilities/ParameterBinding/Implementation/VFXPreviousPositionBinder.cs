using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Previous Position Binder")]
    [VFXBinder("Transform/Position (Previous)")]
    public class VFXPreviousPositionBinder : VFXBinderBase
    {
        [VFXParameterBinding("UnityEngine.Vector3")]
        public ExposedParameter m_Parameter = "PreviousPosition";
        public Transform Target;
        Vector3 oldPosition;
    
        protected override void OnEnable()
        {
            base.OnEnable();
            oldPosition = Target.position;
        }

        public override bool IsValid(VisualEffect component)
        {
            return component.HasVector3(m_Parameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            component.SetVector3(m_Parameter, oldPosition);
            oldPosition = Target.position;
        }

        public override string ToString()
        {
            return string.Format("Previous Position : '{0}' -> {1}", m_Parameter, Target == null ? "(null)" : Target.name);
        }
    }
}

