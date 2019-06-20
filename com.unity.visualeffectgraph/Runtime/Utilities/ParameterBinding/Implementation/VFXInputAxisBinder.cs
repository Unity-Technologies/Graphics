using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Input Axis Binder")]
    [VFXBinder("Input/Axis")]
    public class VFXInputAxisBinder : VFXBinderBase
    {
        public string AxisParameter { get { return (string)m_AxisParameter; } set { m_AxisParameter = value; } }

        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_AxisParameter = "Axis";

        public string AxisName = "Horizontal";
        public float AccumulateSpeed = 1.0f;
        public bool Accumulate = true;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasFloat(m_AxisParameter);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            float axis = Input.GetAxisRaw(AxisName);

            if (Accumulate)
            {
                float value = component.GetFloat(m_AxisParameter);
                component.SetFloat(m_AxisParameter, value + (AccumulateSpeed * axis * Time.deltaTime));
            }
            else
                component.SetFloat(m_AxisParameter, axis);
        }

        public override string ToString()
        {
            return string.Format("Input Axis: '{0}' -> {1}", m_AxisParameter, AxisName.ToString());
        }
    }
}
