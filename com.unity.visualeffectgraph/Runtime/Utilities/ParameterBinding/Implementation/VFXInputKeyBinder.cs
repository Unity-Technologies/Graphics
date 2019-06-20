using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX.Utility
{
    [AddComponentMenu("VFX/Utilities/Parameters/VFX Input Key Press Binder")]
    [VFXBinder("Input/Key")]
    public class VFXInputKeyBinder : VFXBinderBase
    {
        public string KeyParameter { get { return (string)m_KeyParameter; } set { m_KeyParameter = value; } }

        [VFXParameterBinding("System.Boolean"), SerializeField]
        protected ExposedParameter m_KeyParameter = "KeyDown";

        public string KeySmoothParameter { get { return (string)m_KeySmoothParameter; } set { m_KeySmoothParameter = value; } }

        [VFXParameterBinding("System.Single"), SerializeField]
        protected ExposedParameter m_KeySmoothParameter = "KeySmooth";

        public KeyCode Key = KeyCode.Space;
        public float SmoothSpeed = 2.0f;
        public bool UseKeySmooth = true;

        float m_CachedSmoothValue = 0.0f;

        public override bool IsValid(VisualEffect component)
        {
            return component.HasBool(m_KeyParameter) && (UseKeySmooth ? component.HasFloat(m_KeySmoothParameter) : true);
        }

        private void Start()
        {
            if (UseKeySmooth)
            {
                m_CachedSmoothValue = Input.GetKeyDown(Key) ? 1.0f : 0.0f;
            }
        }

        public override void UpdateBinding(VisualEffect component)
        {
            bool press = Input.GetKey(Key);
            component.SetBool(m_KeyParameter, press);
            if (UseKeySmooth)
            {
                m_CachedSmoothValue += SmoothSpeed * Time.deltaTime * (press ? 1.0f : -1.0f);
                m_CachedSmoothValue = Mathf.Clamp01(m_CachedSmoothValue);
                component.SetFloat(m_KeySmoothParameter, m_CachedSmoothValue);
            }
        }

        public override string ToString()
        {
            return string.Format("Key: '{0}' -> {1}", m_KeySmoothParameter, Key.ToString());
        }
    }
}
