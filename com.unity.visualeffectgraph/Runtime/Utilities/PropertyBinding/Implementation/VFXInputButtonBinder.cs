using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Input Button Binder")]
    [VFXBinder("Input/Button")]
    class VFXInputButtonBinder : VFXBinderBase
    {
        public string ButtonProperty { get { return (string)m_ButtonProperty; } set { m_ButtonProperty = value; } }

        [VFXPropertyBinding("System.Boolean"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonParameter")]
        protected ExposedProperty m_ButtonProperty = "ButtonDown";

        public string ButtonSmoothProperty { get { return (string)m_ButtonSmoothProperty; } set { m_ButtonSmoothProperty = value; } }

        [VFXPropertyBinding("System.Single"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_ButtonSmoothParameter")]
        protected ExposedProperty m_ButtonSmoothProperty = "KeySmooth";

        public string ButtonName = "Action";
        public float SmoothSpeed = 2.0f;
        public bool UseButtonSmooth = true;

#if ENABLE_LEGACY_INPUT_MANAGER
        float m_CachedSmoothValue = 0.0f;
#endif

        public override bool IsValid(VisualEffect component)
        {
            return component.HasBool(m_ButtonProperty) && (UseButtonSmooth ? component.HasFloat(m_ButtonSmoothProperty) : true);
        }

        private void Start()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UseButtonSmooth)
            {
                m_CachedSmoothValue = Input.GetButton(ButtonName) ? 1.0f : 0.0f;
            }
#endif
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            bool press = Input.GetButton(ButtonName);
            component.SetBool(m_ButtonProperty, press);
            if (UseButtonSmooth)
            {
                m_CachedSmoothValue += SmoothSpeed * Time.deltaTime * (press ? 1.0f : -1.0f);
                m_CachedSmoothValue = Mathf.Clamp01(m_CachedSmoothValue);
                component.SetFloat(m_ButtonSmoothProperty, m_CachedSmoothValue);
            }
#endif
        }

        public override string ToString()
        {
            return string.Format("Input Button: '{0}' -> {1}", m_ButtonSmoothProperty, ButtonName.ToString());
        }
    }
}
