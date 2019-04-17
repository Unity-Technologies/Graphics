using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Input Button Binder")]
    [VFXBinder("Input/Button")]
    public class VFXInputButtonBinder : VFXBinderBase
    {
        public string ButtonParameter { get { return (string)m_ButtonParameter; } set { m_ButtonParameter = value; } }

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_ButtonParameter = "ButtonDown";

        public string ButtonSmoothParameter { get { return (string)m_ButtonSmoothParameter; } set { m_ButtonSmoothParameter = value; } }

        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_ButtonSmoothParameter = "KeySmooth";

        public string ButtonName = "Action";
        public float SmoothSpeed = 2.0f;
        public bool UseButtonSmooth = true;

        float m_CachedSmoothValue = 0.0f;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasBool(m_ButtonParameter) && (UseButtonSmooth ? component.HasFloat(m_ButtonSmoothParameter) : true);
        }

        private void Start()
        {
            if (UseButtonSmooth)
            {
                m_CachedSmoothValue = Input.GetButton(ButtonName) ? 1.0f : 0.0f;
            }
        }

        public override void UpdateBinding(VisualEffect component)
        {
            bool press = Input.GetButton(ButtonName);
            component.SetBool(m_ButtonParameter, press);
            if (UseButtonSmooth)
            {
                m_CachedSmoothValue += SmoothSpeed * Time.deltaTime * (press ? 1.0f : -1.0f);
                m_CachedSmoothValue = Mathf.Clamp01(m_CachedSmoothValue);
                component.SetFloat(m_ButtonSmoothParameter, m_CachedSmoothValue);
            }
        }

        public override string ToString()
        {
            return string.Format("Input Button: '{0}' -> {1}", m_ButtonSmoothParameter, ButtonName.ToString());
        }
    }
}
