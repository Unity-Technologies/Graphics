using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Input Axis Binder")]
    [VFXBinder("Input/Axis")]
    class VFXInputAxisBinder : VFXBinderBase
    {
        public string AxisProperty { get { return (string)m_AxisProperty; } set { m_AxisProperty = value; } }

        [VFXPropertyBinding("System.Single"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_AxisParameter")]
        protected ExposedProperty m_AxisProperty = "Axis";

        public string AxisName = "Horizontal";
        public float AccumulateSpeed = 1.0f;
        public bool Accumulate = true;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasFloat(m_AxisProperty);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            float axis = Input.GetAxisRaw(AxisName);

            if (Accumulate)
            {
                float value = component.GetFloat(m_AxisProperty);
                component.SetFloat(m_AxisProperty, value + (AccumulateSpeed * axis * Time.deltaTime));
            }
            else
                component.SetFloat(m_AxisProperty, axis);
        }

        public override string ToString()
        {
            return string.Format("Input Axis: '{0}' -> {1}", m_AxisProperty, AxisName.ToString());
        }
    }
}
